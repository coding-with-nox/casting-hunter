using CastingRadar.Application.DTOs;
using CastingRadar.Application.Interfaces;
using CastingRadar.Application.UseCases.UpdateUserProfile;

namespace CastingRadar.Api.Endpoints;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profile");

        group.MapGet("/", async (IUserProfileRepository repo, CancellationToken ct) =>
        {
            var profile = await repo.GetAsync(ct);
            return Results.Ok(UserProfileDto.FromEntity(profile));
        });

        group.MapPut("/", async (UpdateUserProfileRequest request, UpdateUserProfileHandler handler, CancellationToken ct) =>
        {
            var updated = await handler.HandleAsync(request, ct);
            return Results.Ok(updated);
        });

        return app;
    }
}
