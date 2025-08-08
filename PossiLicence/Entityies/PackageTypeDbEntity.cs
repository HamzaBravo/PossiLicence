namespace PossiLicence.Entityies;

public sealed class PackageTypeDbEntity:BaseDbEntity
{
    public string Caption { get; set; } // paket başlığı
    public int MonthCount { get; set; } // ay sayısı
    public int? DayCount { get; set; } // gün sayısı (opsiyonel)
    public decimal Price { get; set; } // paket fiyatı
    public string Description { get; set; } // paket açıklaması
    public bool IsDeleted { get; set; } = false; // silinmiş mi
}
