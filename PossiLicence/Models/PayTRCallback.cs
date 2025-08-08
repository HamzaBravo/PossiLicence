namespace PossiLicence.Models;

public class PayTRCallback
{
    public string merchant_oid { get; set; }
    public string status { get; set; }
    public string total_amount { get; set; }
    public string hash { get; set; }
    public string failed_reason_code { get; set; }
    public string failed_reason_msg { get; set; }
    public string test_mode { get; set; }
    public string payment_type { get; set; }
    public string currency { get; set; }
    public string payment_amount { get; set; }
}
