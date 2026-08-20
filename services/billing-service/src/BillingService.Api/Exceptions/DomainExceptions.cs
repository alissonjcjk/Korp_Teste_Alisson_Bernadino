using BillingService.Api.Models;

namespace BillingService.Api.Exceptions;

/// <summary>
/// Exceção base do domínio de faturamento.
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

/// <summary>Lançada quando a nota fiscal não existe no banco de dados.</summary>
public class InvoiceNotFoundException : DomainException
{
    public InvoiceNotFoundException(Guid id)
        : base($"Nota fiscal com Id '{id}' não encontrada.", statusCode: 404) { }

    public InvoiceNotFoundException(int number)
        : base($"Nota fiscal número '{number}' não encontrada.", statusCode: 404) { }
}

/// <summary>
/// Lançada quando se tenta imprimir uma nota com status diferente de Aberta.
/// Regra de negócio central: somente notas Abertas podem ser impressas.
/// </summary>
public class InvalidInvoiceStatusException : DomainException
{
    public InvalidInvoiceStatusException(int invoiceNumber, InvoiceStatus currentStatus)
        : base($"A nota fiscal #{invoiceNumber} não pode ser impressa pois está com status '{currentStatus}'. " +
               $"Somente notas com status 'Open' (Aberta) podem ser impressas.") { }
}

/// <summary>Lançada quando a nota fiscal não possui nenhum item.</summary>
public class InvoiceHasNoItemsException : DomainException
{
    public InvoiceHasNoItemsException()
        : base("A nota fiscal deve ter ao menos um item antes de ser salva.") { }
}

/// <summary>
/// Lançada quando o Serviço de Estoque está indisponível ou retorna erro.
/// Permite ao controller retornar uma mensagem clara ao usuário.
/// </summary>
public class InventoryServiceUnavailableException : DomainException
{
    public InventoryServiceUnavailableException(string detail = "")
        : base($"O Serviço de Estoque está indisponível no momento. " +
               $"A impressão não pode ser concluída. {detail}".Trim(),
               statusCode: 503) { }
}

/// <summary>
/// Lançada quando o estoque de algum produto é insuficiente para emitir a nota.
/// </summary>
public class InsufficientStockException : DomainException
{
    public InsufficientStockException(string productCode, decimal requested, decimal available)
        : base($"Estoque insuficiente para o produto '{productCode}'. " +
               $"Solicitado: {requested:N4}, Disponível: {available:N4}.") { }
}

/// <summary>
/// Lançada quando a chave de idempotência já foi utilizada,
/// indicando que esta operação já foi processada anteriormente.
/// </summary>
public class DuplicateIdempotencyKeyException : DomainException
{
    public DuplicateIdempotencyKeyException(string key)
        : base($"A requisição com a chave de idempotência '{key}' já foi processada anteriormente. " +
               $"A nota fiscal não será impressa novamente.", statusCode: 409) { }
}
