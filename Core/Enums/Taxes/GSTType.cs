namespace Core.Enums;

public enum GSTType
{
    IGST = 0,   // Inter-state: single tax
    CGST = 1,   // Intra-state component 1
    SGST = 2,   // Intra-state component 2
    UTGST = 3,  // Union territory GST
    Exempt = 4, // Zero-rated / exempt
    Composition = 5
}
