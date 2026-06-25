using Core.Common;
using Core.Enums;

namespace Core.Entities;

public class CreditNote : BaseEntity
{
    public string CreditNoteNumber { get; set; } = null!;
    public DateTime CreditNoteDate { get; set; }
    public string? Reason { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsApplied { get; set; } = false;

    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

}
