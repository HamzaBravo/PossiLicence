namespace PossiLicence.Models;

public class PayTRTokenRequest
{
    public string merchant_id { get; set; }
    public string user_ip { get; set; }
    public string merchant_oid { get; set; }
    public string email { get; set; }
    public int payment_amount { get; set; }
    public string currency { get; set; } = "TL";
    public string user_basket { get; set; }
    public int no_installment { get; set; } = 0;
    public int max_installment { get; set; } = 0;
    public string user_name { get; set; }
    public string user_address { get; set; }
    public string user_phone { get; set; }
    public string merchant_ok_url { get; set; }
    public string merchant_fail_url { get; set; }
    public int test_mode { get; set; } = 1;
    public int debug_on { get; set; } = 1;
    public int timeout_limit { get; set; } = 30;
    public string lang { get; set; } = "tr";
    public string paytr_token { get; set; }
}
