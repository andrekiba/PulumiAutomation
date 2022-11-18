using Pulumi;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;
using System.Collections.Generic;
using Deployment = Pulumi.Deployment;

return await Deployment.RunAsync(() =>
{
    const string projectName = "automation";
    var stackName = Deployment.Instance.StackName;
    var env = stackName.Split("-")[0];
    
    #region Resource Group

    var resourceGroupName = $"{projectName}-{env}-rg";
    var resourceGroup = new ResourceGroup(resourceGroupName, new ResourceGroupArgs
    {
        ResourceGroupName = resourceGroupName
    });

    #endregion

    #region Storage Account

    var storageAccountName = $"{projectName}{env}st".Replace("-", string.Empty);
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
    
    var storageAccountKeys = ListStorageAccountKeys.Invoke(new ListStorageAccountKeysInvokeArgs
    {
        ResourceGroupName = resourceGroup.Name,
        AccountName = storageAccount.Name
    });

    var primaryStorageKey = storageAccountKeys.Apply(accountKeys =>
    {
        var firstKey = accountKeys.Keys[0].Value;
        return Output.CreateSecret(firstKey);
    });
    
    #endregion
    
    // Outputs
    return new Dictionary<string, object?>
    {
        ["primaryStorageKey"] = primaryStorageKey
    };
});