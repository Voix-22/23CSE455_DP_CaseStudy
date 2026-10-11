using FlemanApi.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlemanApi.Controllers;

[ApiController]
[Route("api/addons")]
[AllowAnonymous]
public class AddonController : ControllerBase
{
    private readonly IAddonService _addonService;

    public AddonController(IAddonService addonService)
    {
        _addonService = addonService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAddons() => Ok(await _addonService.GetAddonsAsync());
}
