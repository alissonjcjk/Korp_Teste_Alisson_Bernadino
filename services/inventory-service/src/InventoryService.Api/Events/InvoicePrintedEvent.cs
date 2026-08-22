namespace Korp.Shared.Events;

public record InvoiceItemEvent(Guid ProductId, decimal Quantity);

public record InvoicePrintedEvent(Guid InvoiceId, string InvoiceNumber, List<InvoiceItemEvent> Items);
