using FlemanApi.DTO;
using FlemanApi.Models;
using FlemanApi.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlemanApi.Controllers;

// CH02 - a separate controller on purpose: VehicleController carries a
// class-level [AllowAnonymous], which would override [Authorize] on any
// action added to it, so the staff-only create endpoint cannot live there.
[ApiController]
public class CarTypeController : ControllerBase
{
    private readonly ICarTypeService _carTypeService;

    public CarTypeController(ICarTypeService carTypeService)
    {
        _carTypeService = carTypeService;
    }

    [HttpGet("api/car-types/category/{category}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByCategory(VehicleCategory category) =>
        Ok(await _carTypeService.GetCarTypesByCategoryAsync(category));

    [HttpPost("api/staff/car-types")]
    [Authorize(Roles = "STAFF")]
    public async Task<IActionResult> CreateCarType([FromBody] CreateCarTypeRequest request) =>
        Ok(await _carTypeService.CreateCarTypeAsync(request));
}
