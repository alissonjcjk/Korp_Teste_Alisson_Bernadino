using BillingService.Api.DTOs;
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
