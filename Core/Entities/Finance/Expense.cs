using Core.Common;
using Core.Enums;

namespace Core.Entities;

public class Expense : BaseEntity
{
    public string Title { get; set; } = null!;
    public ExpenseCategory Category { get; set; }
    public DateTime ExpenseDate { get; set; }
    public decimal Amount { get; set; }
    public decimal? GSTAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? VendorName { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public string? ReceiptUrl { get; set; }  // Uploaded receipt file


    public Tenant Tenant { get; set; } = null!;
    public Guid? RecordedBy { get; set; }
    public User? RecordedByUser { get; set; }
}
