namespace PossiLicence.Entityies;

// paket satın alma bilgilerini tutar
public sealed class CompanyPurchaseDbEntity:BaseDbEntity
{
    public Guid CompanyId { get; set; } // Firma id si
    public Guid PackageTypeId { get; set; } // Paket tipi id si
    public bool? Status { get; set; }// Paket satın alma durumu başarılı yada başarısız
    public string Description { get; set; } // Paket satın alma açıklaması başarılı yada başarısız detayları
}
