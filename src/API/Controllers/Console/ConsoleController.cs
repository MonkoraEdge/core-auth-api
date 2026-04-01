using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace MonkoraEdge.Core.Auth.API.Controllers;

[Route("console")]
[ApiController]
[ApiVersion("1.0")]
public partial class ConsoleController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public ConsoleController(
        ITenantService tenantService)
    {
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
    }
}