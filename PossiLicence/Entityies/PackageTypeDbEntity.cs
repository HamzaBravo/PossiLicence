namespace PossiLicence.Entityies;

public sealed class PackageTypeDbEntity:BaseDbEntity
{
    public string Key { get; set; } // paket başlığı
    public int MonthCount { get; set; } // ay sayısı
    public int? DayCount { get; set; } // gün sayısı (opsiyonel)
    public string Description { get; set; } // paket açıklaması
    public bool IsDeleted { get; set; } = false; // silinmiş mi
}
