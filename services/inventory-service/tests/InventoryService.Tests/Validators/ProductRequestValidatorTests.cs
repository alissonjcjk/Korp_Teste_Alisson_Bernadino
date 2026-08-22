using InventoryService.Api.DTOs;
using InventoryService.Api.Validators;

namespace InventoryService.Tests.Validators;

public class CreateProductRequestValidatorTests
{
    private readonly CreateProductRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_IsValid()
    {
        var request = new CreateProductRequest
        {
            Code = "PROD-001",
            Description = "Produto de teste",
            StockBalance = 10.5m,
            Unit = "UN"
        };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "Descrição válida", nameof(CreateProductRequest.Code))]
    [InlineData("PROD-001", "", nameof(CreateProductRequest.Description))]
    public void Validate_WithMissingRequiredText_IsInvalid(
        string code,
        string description,
        string expectedProperty)
    {
        var request = new CreateProductRequest
        {
            Code = code,
            Description = description,
            StockBalance = 0,
            Unit = "UN"
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == expectedProperty);
    }

    [Fact]
    public void Validate_WithNegativeStockBalance_IsInvalid()
    {
        var request = ValidRequest() with { StockBalance = -0.0001m };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            error => error.PropertyName == nameof(CreateProductRequest.StockBalance));
    }

    [Fact]
    public void Validate_WhenStockBalanceIsOmitted_AcceptsDefaultZero_AsCurrentBehavior()
    {
        var request = new CreateProductRequest
        {
            Code = "PROD-001",
            Description = "Produto sem saldo explícito"
        };

        var result = _validator.Validate(request);

        Assert.Equal(0m, request.StockBalance);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenUnitIsNull_AcceptsIt_AsCurrentValidatorBehavior()
    {
        var request = ValidRequest() with { Unit = null! };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    private static CreateProductRequest ValidRequest() => new()
    {
        Code = "PROD-001",
        Description = "Produto de teste",
        StockBalance = 10,
        Unit = "UN"
    };
}

public class UpdateProductRequestValidatorTests
{
    private readonly UpdateProductRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_IsValid()
    {
        var request = new UpdateProductRequest
        {
            Description = "Descrição atualizada",
            Unit = "CX"
        };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyDescription_IsInvalid()
    {
        var request = new UpdateProductRequest { Description = "", Unit = "UN" };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            error => error.PropertyName == nameof(UpdateProductRequest.Description));
    }

    [Fact]
    public void Validate_WithUnitLongerThanTwentyCharacters_IsInvalid()
    {
        var request = new UpdateProductRequest
        {
            Description = "Descrição válida",
            Unit = new string('X', 21)
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            error => error.PropertyName == nameof(UpdateProductRequest.Unit));
    }

    [Fact]
    public void Validate_WhenUnitIsNull_AcceptsIt_AsCurrentValidatorBehavior()
    {
        var request = new UpdateProductRequest
        {
            Description = "Descrição válida",
            Unit = null!
        };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }
}

public class DeductStockRequestValidatorTests
{
    private readonly DeductStockRequestValidator _validator = new();

    [Fact]
    public void Validate_WithPositiveQuantityAndInvoiceReference_IsValid()
    {
        var request = new DeductStockRequest
        {
            Quantity = 2.5m,
            InvoiceReference = "NF-100"
        };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void Validate_WithNonPositiveQuantity_IsInvalid(string rawQuantity)
    {
        var request = new DeductStockRequest
        {
            Quantity = decimal.Parse(rawQuantity, System.Globalization.CultureInfo.InvariantCulture),
            InvoiceReference = "NF-100"
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            error => error.PropertyName == nameof(DeductStockRequest.Quantity));
    }

    [Fact]
    public void Validate_WithEmptyInvoiceReference_IsInvalid()
    {
        var request = new DeductStockRequest { Quantity = 1, InvoiceReference = "" };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            error => error.PropertyName == nameof(DeductStockRequest.InvoiceReference));
    }
}
