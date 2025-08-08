namespace PossiLicence.Entityies;

public sealed class CompanyDbEntity:BaseDbEntity
{
    public int UniqId { get; set; } // Firma için benzersiz id program tarafından sorgulanacak
    public string CompanyName { get; set; } // Firma adı
    public string FullName { get; set; } // Firma yetkilisi adı
    public string PhoneNumber { get; set; } // Firma telefon numarası
    public DateTime? EndDate { get; set; }  // Firma için satın alınan paketin son kullanım tarihi
}
