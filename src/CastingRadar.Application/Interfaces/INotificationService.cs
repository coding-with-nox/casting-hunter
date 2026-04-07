using CastingRadar.Domain.Entities;

namespace CastingRadar.Application.Interfaces;

public interface INotificationService
{
    Task SendAsync(CastingCall castingCall, CancellationToken cancellationToken = default);
}
