namespace PossiLicence.Dtos;

public class UpdateCompanyRequest
{
    public string CompanyName { get; set; }
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public string Notes { get; set; } // Yeni eklenen
    public List<string> PackageIds { get; set; } = new List<string>(); // Yeni eklenen
}
