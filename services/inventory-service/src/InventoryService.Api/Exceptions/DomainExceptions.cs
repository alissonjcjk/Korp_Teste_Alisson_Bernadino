namespace InventoryService.Api.Exceptions;

/// <summary>
/// Exceção base do domínio de estoque.
/// Todas as exceções de negócio herdam desta.
/// </summary>
public abstract class DomainException : Exception
{
    public int StatusCode { get; }

    protected DomainException(string message, int statusCode = 400)
        : base(message)
    {
        StatusCode = statusCode;
    }
}

/// <summary>
/// Lançada quando um produto solicitado não existe no banco de dados.
/// </summary>
public class ProductNotFoundException : DomainException
{
    public ProductNotFoundException(Guid id)
        : base($"Produto com Id '{id}' não encontrado.", statusCode: 404) { }

    public ProductNotFoundException(string code)
        : base($"Produto com código '{code}' não encontrado.", statusCode: 404) { }
}

/// <summary>
/// Lançada quando o estoque disponível é insuficiente para o abatimento solicitado.
/// </summary>
public class InsufficientStockException : DomainException
{
    public InsufficientStockException(string productCode, decimal requested, decimal available)
        : base($"Estoque insuficiente para o produto '{productCode}'. " +
               $"Solicitado: {requested}, Disponível: {available}.") { }
}

/// <summary>
/// Lançada quando há tentativa de cadastrar um produto com código já existente.
/// </summary>
public class DuplicateProductCodeException : DomainException
{
    public DuplicateProductCodeException(string code)
        : base($"Já existe um produto cadastrado com o código '{code}'.") { }
}

/// <summary>
/// Lançada quando ocorre conflito de concorrência otimista (dois processos
/// tentaram modificar o mesmo produto simultaneamente).
/// </summary>
public class ConcurrencyConflictException : DomainException
{
    public ConcurrencyConflictException(string productCode)
        : base($"Conflito de concorrência ao atualizar o produto '{productCode}'. " +
               $"Por favor, tente novamente.", statusCode: 409) { }
}
