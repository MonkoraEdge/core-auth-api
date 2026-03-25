using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace MonkoraEdge.Core.Auth.API.Controllers;

[Route("console")]
[ApiController]
public partial class ConsoleController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public ConsoleController(
        ITenantService tenantService)
    {
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
    }
}