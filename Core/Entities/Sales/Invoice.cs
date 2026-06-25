using Core.Common;
using Core.Enums;

namespace Core.Entities;

public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = null!;
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    // GST Fields
    public bool IsInterState { get; set; } = false;
    public string? PlaceOfSupply { get; set; }      // State name
    public string? PlaceOfSupplyCode { get; set; }  // State code

    // Amounts (all stored tax-exclusive, tax computed and stored)
    public decimal SubTotal { get; set; }       // Before any discount or tax
    public decimal DiscountAmount { get; set; } = 0;
    public DiscountType DiscountType { get; set; } = DiscountType.None;
    public decimal? DiscountPercent { get; set; }

    // Tax breakdown (GST)
    public decimal TaxableAmount { get; set; }  // SubTotal − Discount
    public decimal IGSTAmount { get; set; } = 0;
    public decimal CGSTAmount { get; set; } = 0;
    public decimal SGSTAmount { get; set; } = 0;
    public decimal CessAmount { get; set; } = 0;
    public decimal TotalTaxAmount { get; set; }

    // Final
    public decimal RoundOff { get; set; } = 0;
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; } = 0;
    public decimal BalanceDue { get; set; }

    // Additional Info
    public string? Notes { get; set; }
    public string? TermsAndConditions { get; set; }
    public string? ShippingDetails { get; set; }
    public string? VehicleNumber { get; set; }
    public string? EWayBillNumber { get; set; }
    public string? IRN { get; set; }            // e-Invoice IRN
    public string? AckNumber { get; set; }      // e-Invoice Ack
    public string? QRCode { get; set; }         // e-Invoice QR

    // Relations

    public Tenant Tenant { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public Guid? SalesAgentId { get; set; }
    public User? SalesAgent { get; set; }
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<CreditNote> CreditNotes { get; set; } = new List<CreditNote>();
}
