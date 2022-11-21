namespace Automation.Api.Models;

public class CreateSite
{
    public string SubscriptionId { get; set; }
    public string Location { get; set; }
    public string ProjectName { get; set; }
    public string SiteName { get; set; }
    public string Content { get; set; }
    public string Content404 { get; set; }
}