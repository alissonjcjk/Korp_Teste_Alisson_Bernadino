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
    [InlineData(null, "Descrição válida", nameof(CreateProductRequest.Code))]
    [InlineData("PROD-001", "", nameof(CreateProductRequest.Description))]
    [InlineData("PROD-001", null, nameof(CreateProductRequest.Description))]
    public void Validate_WithMissingRequiredText_IsInvalid(
        string? code,
        string? description,
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
    public void Validate_WithControlCharactersInProductCode_IsInvalid()
    {
        var request = ValidRequest() with { Code = "PROD-001\r\nlog-forjado" };

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(CreateProductRequest.Code) &&
            error.ErrorMessage == "O código do produto contém caracteres inválidos.");
    }

    [Fact]
    public void Validate_WhenStockBalanceIsOmitted_IsInvalid()
    {
        var request = new CreateProductRequest
        {
            Code = "PROD-001",
            Description = "Produto sem saldo explícito",
            Unit = "UN"
        };

        var result = _validator.Validate(request);

        Assert.Null(request.StockBalance);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(CreateProductRequest.StockBalance) &&
            error.ErrorMessage == "O saldo inicial é obrigatório.");
    }

    [Fact]
    public void Validate_WhenUnitIsNull_IsInvalid()
    {
        var request = ValidRequest() with { Unit = null };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(CreateProductRequest.Unit) &&
            error.ErrorMessage == "A unidade é obrigatória.");
    }

    [Theory]
    [InlineData("123456789012345")]
    [InlineData("1.00001")]
    public void Validate_WhenStockBalanceExceedsNumericPrecision_IsInvalid(string rawBalance)
    {
        var request = ValidRequest() with
        {
            StockBalance = decimal.Parse(
                rawBalance,
                System.Globalization.CultureInfo.InvariantCulture)
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(CreateProductRequest.StockBalance) &&
            error.ErrorMessage ==
                "O saldo inicial deve ter no máximo 14 dígitos inteiros e 4 casas decimais.");
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

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithMissingDescription_IsInvalid(string? description)
    {
        var request = new UpdateProductRequest { Description = description, Unit = "UN" };

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
    public void Validate_WhenUnitIsNull_IsInvalid()
    {
        var request = new UpdateProductRequest
        {
            Description = "Descrição válida",
            Unit = null
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(UpdateProductRequest.Unit) &&
            error.ErrorMessage == "A unidade é obrigatória.");
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

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithMissingInvoiceReference_IsInvalid(string? invoiceReference)
    {
        var request = new DeductStockRequest
        {
            Quantity = 1,
            InvoiceReference = invoiceReference
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            error => error.PropertyName == nameof(DeductStockRequest.InvoiceReference));
    }

    [Theory]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("NF-100\r\nlog-forjado")]
    public void Validate_WithUnsafeInvoiceReference_IsInvalid(string invoiceReference)
    {
        var request = new DeductStockRequest
        {
            Quantity = 1,
            InvoiceReference = invoiceReference
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            error => error.PropertyName == nameof(DeductStockRequest.InvoiceReference));
    }

    [Fact]
    public void Validate_WhenQuantityIsOmitted_IsInvalid()
    {
        var request = new DeductStockRequest { InvoiceReference = "NF-100" };

        var result = _validator.Validate(request);

        Assert.Null(request.Quantity);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(DeductStockRequest.Quantity) &&
            error.ErrorMessage == "A quantidade é obrigatória.");
    }

    [Theory]
    [InlineData("123456789012345")]
    [InlineData("1.00001")]
    public void Validate_WhenQuantityExceedsNumericPrecision_IsInvalid(string rawQuantity)
    {
        var request = new DeductStockRequest
        {
            Quantity = decimal.Parse(
                rawQuantity,
                System.Globalization.CultureInfo.InvariantCulture),
            InvoiceReference = "NF-100"
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(DeductStockRequest.Quantity) &&
            error.ErrorMessage ==
                "A quantidade deve ter no máximo 14 dígitos inteiros e 4 casas decimais.");
    }
}
