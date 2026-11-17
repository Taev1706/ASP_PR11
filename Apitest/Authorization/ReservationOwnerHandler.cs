using System.Security.Claims;
using Apitest.Common;
using Apitest.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace Apitest.Authorization;

public sealed class ReservationOwnerRequirement : IAuthorizationRequirement
{
}

public sealed class ReservationOwnerHandler : AuthorizationHandler<ReservationOwnerRequirement, ProductReservationDto>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        ReservationOwnerRequirement requirement, ProductReservationDto reservation)
    {
        if (context.User.IsInRole(AccessRules.Admin) ||
            (context.User.IsInRole(AccessRules.Customer) &&
             int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) &&
             userId == reservation.UserId))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
