namespace PossiLicence.Dtos;

public class UpdateAdminRequest
{
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public bool IsSuperAdmin { get; set; } = false;
    public List<string> Permissions { get; set; } = new List<string>();
}
