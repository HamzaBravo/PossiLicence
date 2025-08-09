namespace PossiLicence.Dtos;

public class CreateAdminRequest
{
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public string Password { get; set; }
    public bool IsSuperAdmin { get; set; } = false;
    public List<string> Permissions { get; set; } = new List<string>();
}
