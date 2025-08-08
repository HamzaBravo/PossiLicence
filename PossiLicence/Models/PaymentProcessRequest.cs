namespace PossiLicence.Models;

public class PaymentProcessRequest
{
    public int CompanyUniqId { get; set; }
    public Guid PackageId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
}
