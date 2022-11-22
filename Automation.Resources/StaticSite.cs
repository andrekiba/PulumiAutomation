using Pulumi;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;

namespace Automation.Resources;

public class StatiSiteArgs
{
    public ResourceGroup ResourceGroup { get; set; } = null!;
    public string ProjectName { get; set; } = null!;
    public string SiteName { get; set; } = null!;
    public string? IndexDocument  { get; set; }
    public string? ErrorDocument { get; set; }
    public string Content { get; set; } = null!;
    public string Content404 { get; set; } = null!;
}

public class StaticSite : ComponentResource
{
    public Output<string>? StaticEndpoint { get; }

    public StaticSite(string name, StatiSiteArgs args, ComponentResourceOptions? options = null)
        : base("automation:resources:StaticSite", name, options)
    {
        var resourceGroup = args.ResourceGroup;
        var projectName = args.ProjectName;
        var siteName = args.SiteName;
        var indexDocument = args.IndexDocument ?? "index.html";
        var errorDocument = args.ErrorDocument ?? "error.html";

        var storageAccountName = $"{projectName}{siteName}st".Replace("-", string.Empty);
        var storageAccount = new StorageAccount(storageAccountName, new StorageAccountArgs
        {
            AccountName = storageAccountName,
            ResourceGroupName = resourceGroup.Name,
            MinimumTlsVersion = "TLS1_2",
            EnableHttpsTrafficOnly = true,
            AccessTier = AccessTier.Hot,
            AllowBlobPublicAccess = true,
            AllowSharedKeyAccess = true,
            Kind = "StorageV2",
            Sku = new SkuArgs
            {
                Name = "Standard_LRS"
            }
        });
        
        var staticWebSiteName = $"{storageAccountName}-sbs";
        var staticWebsite = new StorageAccountStaticWebsite(staticWebSiteName, new StorageAccountStaticWebsiteArgs
        {
            AccountName = storageAccount.Name,
            ResourceGroupName = resourceGroup.Name,
            IndexDocument = indexDocument,
            Error404Document = errorDocument
        });
        
        var indexHtml = new Blob(indexDocument, new BlobArgs
        {
            ResourceGroupName = resourceGroup.Name,
            AccountName = storageAccount.Name,
            ContainerName = staticWebsite.ContainerName,
            Source = new StringAsset(args.Content),
            ContentType = "text/html"
        });
        var notfoundHtml = new Blob(errorDocument, new BlobArgs
        {
            ResourceGroupName = resourceGroup.Name,
            AccountName = storageAccount.Name,
            ContainerName = staticWebsite.ContainerName,
            Source = new StringAsset(args.Content404),
            ContentType = "text/html"
        });
        
        StaticEndpoint = storageAccount.PrimaryEndpoints.Apply(primaryEndpoints => primaryEndpoints.Web);
    }
}