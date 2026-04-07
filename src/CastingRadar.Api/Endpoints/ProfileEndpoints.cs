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
            // Input validation
            if (request.ScenicAge is < 1 or > 100)
                return Results.BadRequest("ScenicAge must be between 1 and 100.");
            if (request.TelegramChatId is { Length: > 50 })
                return Results.BadRequest("TelegramChatId too long.");
            if (request.Gender is not null && request.Gender.Length > 20)
                return Results.BadRequest("Gender value too long.");

            var updated = await handler.HandleAsync(request, ct);
            return Results.Ok(updated);
        });

        return app;
    }
}
