using Core.Common;
using Core.Enums;

namespace Core.Entities;

public class Payment : BaseEntity
{
    public string PaymentNumber { get; set; } = null!;
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    // Receivable (customer paying us)
    public Guid? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    // Payable (us paying supplier)
    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public string? ReferenceNumber { get; set; }  // UTR / Cheque no / UPI ref
    public string? BankName { get; set; }
    public string? Notes { get; set; }
    public bool IsRefund { get; set; } = false;


    public Tenant Tenant { get; set; } = null!;
    public Guid? RecordedBy { get; set; }
    public User? RecordedByUser { get; set; }
}
