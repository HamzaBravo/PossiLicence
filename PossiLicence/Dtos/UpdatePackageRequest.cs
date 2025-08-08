namespace PossiLicence.Dtos;

public class UpdatePackageRequest
{
    public string Caption { get; set; } 
    public int MonthCount { get; set; }
    public int? DayCount { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; }
}
