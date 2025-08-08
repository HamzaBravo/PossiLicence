namespace PossiLicence.Entityies;

public class OrderDbEntity:BaseDbEntity
{
    public Guid CompanyId { get; set; }
    public Guid PackageId { get; set; }
    public Guid CompanyPurchasesId { get; set; }
    public string IpAdress { get; set; }
    public string OrderNumber { get; set; }
}
