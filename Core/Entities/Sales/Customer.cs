using Core.Common;
using Core.Enums;

namespace Core.Entities;

public class Customer : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AlternatePhone { get; set; }
    public CustomerType CustomerType { get; set; } = CustomerType.Business;
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    // GST & Tax Info
    public string? GSTIN { get; set; }
    public string? PAN { get; set; }
    public bool IsGSTRegistered { get; set; } = false;

    // Address (Billing)
    public string? BillingAddressLine1 { get; set; }
    public string? BillingAddressLine2 { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingState { get; set; }
    public string? BillingStateCode { get; set; }
    public string? BillingPinCode { get; set; }
    public string? BillingCountry { get; set; } = "India";

    // Address (Shipping) — can differ
    public bool ShippingSameAsBilling { get; set; } = true;
    public string? ShippingAddressLine1 { get; set; }
    public string? ShippingAddressLine2 { get; set; }
    public string? ShippingCity { get; set; }
    public string? ShippingState { get; set; }
    public string? ShippingStateCode { get; set; }
    public string? ShippingPinCode { get; set; }
    public string? ShippingCountry { get; set; }

    // Credit & Limits
    public decimal CreditLimit { get; set; } = 0;
    public int? PaymentTermDays { get; set; } = 30;  // Net-30 default
    public string? Notes { get; set; }
    public string? Tags { get; set; }  // CSV tags for segmentation

    // Relations

    public Tenant Tenant { get; set; } = null!;
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
