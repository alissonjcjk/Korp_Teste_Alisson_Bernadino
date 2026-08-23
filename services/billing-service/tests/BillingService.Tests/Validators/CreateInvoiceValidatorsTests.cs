using BillingService.Api.DTOs;
using BillingService.Api.Models;
using BillingService.Api.Validators;

namespace BillingService.Tests.Validators;

public class CreateInvoiceValidatorsTests
{
    private readonly CreateInvoiceItemRequestValidator _itemValidator = new();
    private readonly CreateInvoiceRequestValidator _invoiceValidator = new();

    [Fact]
    public void ItemValidator_AcceptsValidItemIncludingZeroUnitPrice()
    {
        var request = ValidItem() with { UnitPrice = 0m };

        var result = _itemValidator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ItemValidator_AcceptsTrailingZerosBeyondStoredScale()
    {
        var request = ValidItem() with
        {
            Quantity = 2.50000m,
            UnitPrice = 10.12000m
        };

        var result = _itemValidator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ItemValidator_RejectsEmptyProductId()
    {
        var request = ValidItem() with { ProductId = Guid.Empty };

        var result = _itemValidator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateInvoiceItemRequest.ProductId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ItemValidator_RejectsNonPositiveQuantity(int quantity)
    {
        var request = ValidItem() with { Quantity = quantity };

        var result = _itemValidator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateInvoiceItemRequest.Quantity));
    }

    [Fact]
    public void ItemValidator_RejectsNegativeUnitPrice()
    {
        var request = ValidItem() with { UnitPrice = -0.01m };

        var result = _itemValidator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateInvoiceItemRequest.UnitPrice));
    }

    [Fact]
    public void ItemValidator_RejectsMissingQuantityAndUnitPrice()
    {
        var request = new CreateInvoiceItemRequest
        {
            ProductId = Guid.NewGuid(),
            Quantity = null,
            UnitPrice = null
        };

        var result = _itemValidator.Validate(request);

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(CreateInvoiceItemRequest.Quantity) &&
            error.ErrorMessage == "A quantidade é obrigatória.");
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(CreateInvoiceItemRequest.UnitPrice) &&
            error.ErrorMessage == "O preço unitário é obrigatório.");
    }

    [Fact]
    public void ItemValidator_RejectsValuesOutsideNumeric18Scale4()
    {
        var request = ValidItem() with
        {
            Quantity = 123456789012345m,
            UnitPrice = 1.00001m
        };

        var result = _itemValidator.Validate(request);

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(CreateInvoiceItemRequest.Quantity) &&
            error.ErrorMessage == "A quantidade deve ter no máximo 14 dígitos inteiros e 4 casas decimais.");
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(CreateInvoiceItemRequest.UnitPrice) &&
            error.ErrorMessage == "O preço unitário deve ter no máximo 14 dígitos inteiros e 4 casas decimais.");
    }

    [Fact]
    public void ItemValidator_RejectsLineTotalOutsideNumeric18Scale4()
    {
        var request = ValidItem() with
        {
            Quantity = InvoiceAmount.MaxValue,
            UnitPrice = 2m
        };

        var result = _itemValidator.Validate(request);

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(CreateInvoiceItemRequest.UnitPrice) &&
            error.ErrorMessage ==
                "O total do item não pode ultrapassar 14 dígitos inteiros e 4 casas decimais.");
    }

    [Fact]
    public void InvoiceValidator_AcceptsValidRequestWithOptionalFieldsOmitted()
    {
        var request = new CreateInvoiceRequest
        {
            Items = new List<CreateInvoiceItemRequest> { ValidItem() }
        };

        var result = _invoiceValidator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void InvoiceValidator_RejectsInvoiceWithoutItems()
    {
        var request = new CreateInvoiceRequest { Items = new List<CreateInvoiceItemRequest>() };

        var result = _invoiceValidator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateInvoiceRequest.Items));
    }

    [Fact]
    public void InvoiceValidator_RejectsMoreThanOneHundredItems()
    {
        var request = new CreateInvoiceRequest
        {
            Items = Enumerable.Range(0, CreateInvoiceRequestValidator.MaximumItems + 1)
                .Select(_ => ValidItem())
                .ToList()
        };

        var result = _invoiceValidator.Validate(request);

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(CreateInvoiceRequest.Items) &&
            error.ErrorMessage == "A nota fiscal não pode ter mais de 100 itens.");
    }

    [Fact]
    public void InvoiceValidator_WhenItemsIsMissing_ReturnsOnlyFluentValidationError()
    {
        var request = new CreateInvoiceRequest { Items = null };

        var result = _invoiceValidator.Validate(request);

        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(CreateInvoiceRequest.Items), error.PropertyName);
        Assert.Equal("A lista de itens é obrigatória.", error.ErrorMessage);
    }

    [Fact]
    public void InvoiceValidator_RejectsNullItemInsteadOfLettingItReachTheService()
    {
        var request = new CreateInvoiceRequest
        {
            Items = new List<CreateInvoiceItemRequest> { null! }
        };

        var result = _invoiceValidator.Validate(request);

        Assert.Contains(result.Errors, error =>
            error.PropertyName == "Items[0]" &&
            error.ErrorMessage == "O item da nota fiscal não pode ser nulo.");
    }

    [Fact]
    public void InvoiceValidator_RejectsAggregateTotalOutsideNumeric18Scale4()
    {
        var request = new CreateInvoiceRequest
        {
            Items = new List<CreateInvoiceItemRequest>
            {
                ValidItem() with { Quantity = 1m, UnitPrice = 60_000_000_000_000m },
                ValidItem() with { Quantity = 1m, UnitPrice = 60_000_000_000_000m }
            }
        };

        var result = _invoiceValidator.Validate(request);

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(CreateInvoiceRequest.Items) &&
            error.ErrorMessage ==
                "O valor total da nota fiscal não pode ultrapassar 14 dígitos inteiros e 4 casas decimais.");
    }

    [Fact]
    public void InvoiceValidator_RejectsOversizedCustomerNameAndNotes()
    {
        var request = new CreateInvoiceRequest
        {
            CustomerName = new string('C', 256),
            Notes = new string('N', 1001),
            Items = new List<CreateInvoiceItemRequest> { ValidItem() }
        };

        var result = _invoiceValidator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateInvoiceRequest.CustomerName));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateInvoiceRequest.Notes));
    }

    [Fact]
    public void InvoiceValidator_AppliesNestedItemRules()
    {
        var request = new CreateInvoiceRequest
        {
            Items = new List<CreateInvoiceItemRequest>
            {
                ValidItem() with { Quantity = 0m }
            }
        };

        var result = _invoiceValidator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == "Items[0].Quantity");
    }

    private static CreateInvoiceItemRequest ValidItem() => new()
    {
        ProductId = Guid.NewGuid(),
        Quantity = 2m,
        UnitPrice = 10.50m
    };
}
