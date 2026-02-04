namespace DemoCompFuncApp.Models;

public class Customer
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public string Status { get; set; } = "Active";
}
