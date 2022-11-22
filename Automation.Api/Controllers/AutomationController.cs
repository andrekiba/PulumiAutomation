using Automation.Api.Models;
using Automation.Resources;
using Microsoft.AspNetCore.Mvc;
using Pulumi.Automation;
using Pulumi.Automation.Commands.Exceptions;
using Pulumi.AzureNative.Resources;

namespace Automation.Api.Controllers;

[ApiController]
[Route("automation")]
public class AutomationController : ControllerBase
{
    readonly ILogger<AutomationController> logger;

    public AutomationController(ILogger<AutomationController> logger)
    {
        this.logger = logger;
    }
    
    [HttpGet("sites/{projectName}")]
    public async Task<IActionResult> GetSites([FromRoute] string projectName)
    {
        try
        {
            var ws = await LocalWorkspace.CreateAsync(new LocalWorkspaceOptions
            {
                ProjectSettings = new ProjectSettings(projectName, ProjectRuntimeName.Dotnet)
            });
            
            var stacks = await ws.ListStacksAsync();
            var stackTasks = stacks.Select(s =>
                LocalWorkspace.SelectStackAsync(new InlineProgramArgs(projectName, s.Name, PulumiFn.Create(() => { }))));
            
            var stackResults = await Task.WhenAll(stackTasks);

            var sites = stackResults.Select(async s => new Site
            {
                Name = s.Name,
                Endpoint = (await s.GetOutputsAsync())["staticEndpoint"].Value.ToString()
            }).ToList();
            
            return new OkObjectResult(sites);
        }
        catch (Exception e)
        {
           return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }
    
    [HttpGet("sites/{projectName}/{sitName}")]
    public async Task<IActionResult> GetSite([FromRoute] string projectName, [FromRoute] string sitName)
    {
        try
        {
            var stack = await LocalWorkspace.SelectStackAsync(new InlineProgramArgs(projectName, sitName, PulumiFn.Create(() => { })));
            var outputs = await stack.GetOutputsAsync();
            
            return new OkObjectResult(new Site
            {
                Name = sitName,
                Endpoint = outputs["staticEndpoint"].Value.ToString()
            });
        }
        catch (Exception e)
        {
            return e is StackNotFoundException ? 
                new NotFoundObjectResult($"Stack {sitName} does not exist in project {projectName}!") : 
                StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }
    
    [HttpPost("sites")]
    public async Task<IActionResult> CreateSite([FromBody] CreateSite model)
    {
        try
        {
            var program = CreatePulumiProgram(model.ProjectName, model.SiteName, model.Content, model.Content404);

            var stackArgs = new InlineProgramArgs(model.ProjectName, model.SiteName, program);
            var stack = await LocalWorkspace.CreateOrSelectStackAsync(stackArgs);
            await stack.Workspace.InstallPluginAsync("azure-native", "v1.86.0");
            await stack.SetConfigAsync("azure-native:location", new ConfigValue(model.Location));
            await stack.SetConfigAsync("azure-native:subscriptionId", new ConfigValue(model.SubscriptionId));

            var upResult = await stack.UpAsync(new UpOptions
            {
                Logger = logger,
                OnStandardOutput = s => logger.LogInformation(s),
                OnStandardError = s => logger.LogError(s)
            });

            return new OkObjectResult(new Site
            {
                Name = model.SiteName,
                Endpoint = upResult.Outputs["staticEndpoint"].Value.ToString()
            });
        }
        catch (Exception e)
        {
            return e is StackAlreadyExistsException ? 
                new ConflictObjectResult($"Stack {model.SiteName} already exists in project {model.ProjectName}!") : 
                StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }
    
    [HttpPut("sites/{projectName}/{siteName}")]
    public async Task<IActionResult> UpdateSite([FromRoute] string projectName, [FromRoute] string siteName, [FromBody] UpdateSite model)
    {
        try
        {
            var program = CreatePulumiProgram(projectName, siteName, model.Content, model.Content404);
            
            var stack = await LocalWorkspace.SelectStackAsync(new InlineProgramArgs(projectName, siteName, program));
            var upResult = await stack.UpAsync(new UpOptions
            {
                Logger = logger,
                OnStandardOutput = s => logger.LogInformation(s),
                OnStandardError = s => logger.LogError(s)
            });

            return new OkObjectResult(new Site
            {
                Name = siteName,
                Endpoint = upResult.Outputs["staticEndpoint"].Value.ToString()
            });
        }
        catch (Exception e)
        {
            return e switch
            {
                StackNotFoundException => new NotFoundObjectResult(
                    $"Stack {siteName} does not exist in project {projectName}!"),
                ConcurrentUpdateException => new ConflictObjectResult(
                    $"Stack {siteName} already has update in progess!"),
                _ => StatusCode(StatusCodes.Status500InternalServerError, e.Message)
            };
        }
    }
    
    [HttpDelete("sites/{projectName}/{siteName}")]
    public async Task<IActionResult> DeleteSite([FromRoute] string projectName, [FromRoute] string siteName)
    {
        try
        {
            var stack = await LocalWorkspace.SelectStackAsync(new InlineProgramArgs(projectName, siteName, PulumiFn.Create(() => { })));
            await stack.DestroyAsync(new DestroyOptions
            {
                OnStandardOutput = s => logger.LogInformation(s),
                OnStandardError = s => logger.LogError(s)
            });
            await stack.Workspace.RemoveStackAsync(siteName);
            return Ok();
        }
        catch (Exception e)
        {
            return e switch
            {
                StackNotFoundException => new NotFoundObjectResult(
                    $"Stack {siteName} does not exist in project {projectName}!"),
                ConcurrentUpdateException => new ConflictObjectResult(
                    $"Stack {siteName} already has update in progess!"),
                _ => StatusCode(StatusCodes.Status500InternalServerError, e.Message)
            };
        }
    }

    static PulumiFn CreatePulumiProgram(string projectName, string siteName, string content, string content404)
    {
        var program = PulumiFn.Create(() =>
        {
            var resourceGroupName = $"{projectName}-{siteName}-rg";
            var resourceGroup = new ResourceGroup(resourceGroupName, new ResourceGroupArgs
            {
                ResourceGroupName = resourceGroupName
            });

            var staticSiteName = $"{projectName}-{siteName}-ss";
            var staticSite = new StaticSite(staticSiteName, new StatiSiteArgs
            {
                ResourceGroup = resourceGroup,
                ProjectName = projectName,
                SiteName = siteName,
                Content = content,
                Content404 = content404
            });

            return new Dictionary<string, object>
            {
                ["staticEndpoint"] = staticSite.StaticEndpoint
            };
        });
        return program;
    }
}