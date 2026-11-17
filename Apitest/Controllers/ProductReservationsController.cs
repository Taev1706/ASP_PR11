using System.Globalization;
using System.Security.Claims;
using Apitest.Common;
using Microsoft.AspNetCore.Authorization;
using Apitest.Dtos;
using Apitest.Services;
using Microsoft.AspNetCore.Mvc;

namespace Apitest.Controllers;

[ApiController]
[Authorize(Roles = AccessRules.Admin + "," + AccessRules.Customer)]
[Route("api/productreservations")]
public class ProductReservationsController(IProductReservationService service, IAuthorizationService authorization) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AccessRules.AdminOnly)]
    public async Task<ActionResult<List<ProductReservationDto>>> GetAll(int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var result = await service.GetAllAsync(page, pageSize, ct);
        Response.Headers["X-Total-Count"] = result.TotalCount.ToString(CultureInfo.InvariantCulture);
        return Ok(result.Items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductReservationDto>> GetById(int id, CancellationToken ct)
    {
        var reservation = await service.GetByIdAsync(id, ct);
        return await CanAccessAsync(reservation) ? Ok(reservation) : Forbid();
    }

    [HttpPost]
    public async Task<ActionResult<ProductReservationDto>> Create(CreateProductReservationDto dto, CancellationToken ct)
    {
        if (!User.IsInRole(AccessRules.Admin) &&
            (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || dto.UserId != userId))
            return Forbid();

        var created = await service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.ProductReservationId }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateProductReservationDto dto, CancellationToken ct)
    {
        if (!await CanAccessAsync(await service.GetByIdAsync(id, ct)))
            return Forbid();

        await service.UpdateAsync(id, dto, ct);
        return NoContent();
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Patch(int id, PatchProductReservationDto dto, CancellationToken ct)
    {
        if (!await CanAccessAsync(await service.GetByIdAsync(id, ct)))
            return Forbid();

        await service.PatchAsync(id, dto, ct);
        return NoContent();
    }

    [Authorize(Policy = AccessRules.AdminOnly)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        if (!await CanAccessAsync(await service.GetByIdAsync(id, ct)))
            return Forbid();

        await service.CancelAsync(id, ct);
        return NoContent();
    }

    private async Task<bool> CanAccessAsync(ProductReservationDto reservation) =>
        (await authorization.AuthorizeAsync(User, reservation, AccessRules.ReservationOwner)).Succeeded;
}
