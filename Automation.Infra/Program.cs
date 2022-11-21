using Pulumi;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;
using System.Collections.Generic;
using System.IO;
using Automation.Resources;
using Deployment = Pulumi.Deployment;
using Storage = Pulumi.AzureNative.Storage;

return await Deployment.RunAsync(() =>
{
    var projectName = Deployment.Instance.ProjectName;
    var stackName = Deployment.Instance.StackName;

    #region Resource Group

    var resourceGroupName = $"{projectName}-{stackName}-rg";
    var resourceGroup = new ResourceGroup(resourceGroupName, new ResourceGroupArgs
    {
        ResourceGroupName = resourceGroupName
    });

    #endregion

    #region Storage Account
    
    var storageAccountName = $"{projectName}{stackName}st".Replace("-", string.Empty);
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
    
    var staticWebSiteName = $"{storageAccount.Name}-sbs";
    var staticWebsite = new StorageAccountStaticWebsite(staticWebSiteName, new StorageAccountStaticWebsiteArgs
    {
        AccountName = storageAccount.Name,
        ResourceGroupName = resourceGroup.Name,
        IndexDocument = "index.html",
        Error404Document = "404.html"
    });

    var indexHtml = new Blob("index.html", new BlobArgs
    {
        ResourceGroupName = resourceGroup.Name,
        AccountName = storageAccount.Name,
        ContainerName = staticWebsite.ContainerName,
        Source = new FileAsset("./wwwroot/index.html"),
        ContentType = "text/html"
    });
    var notfoundHtml = new Blob("404.html", new BlobArgs
    {
        ResourceGroupName = resourceGroup.Name,
        AccountName = storageAccount.Name,
        ContainerName = staticWebsite.ContainerName,
        Source = new FileAsset("./wwwroot/404.html"),
        ContentType = "text/html"
    });
    
    var staticEndpoint = storageAccount.PrimaryEndpoints.Apply(primaryEndpoints => primaryEndpoints.Web);
    
    #endregion

    #region StaticSite
    /*
    var staticSiteName = $"{projectName}-{stackName}-ss";
    var staticSite = new StaticSite(staticSiteName, new StatiSiteArgs
    {
        ResourceGroup = resourceGroup,
        ProjectName = projectName,
        SiteName = stackName,
        Content = File.ReadAllText("./wwwroot/index.html"),
        Content404 = File.ReadAllText("./wwwroot/404.html")
    }); 
    */
    #endregion
    
    // Outputs
    return new Dictionary<string, object?>
    {
        ["staticEndpoint"] = staticEndpoint,
        //["staticEndpoint"] = staticSite.StaticEndpoint
    };
});