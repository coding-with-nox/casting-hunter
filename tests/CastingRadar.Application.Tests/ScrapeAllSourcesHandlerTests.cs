using CastingRadar.Application.Interfaces;
using CastingRadar.Application.UseCases.ScrapeAllSources;
using CastingRadar.Domain.Entities;
using CastingRadar.Domain.Enums;
using CastingRadar.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CastingRadar.Application.Tests;

public class ScrapeAllSourcesHandlerTests
{
    private static CastingCall MakeCall(string title = "Test", string url = "https://example.com/1") =>
        CastingCall.Create(title, "Description", url, "TestSource", CastingType.Film, SourceRegion.Italy);

    [Fact]
    public async Task HandleAsync_NewCall_SavedAndNotified()
    {
        var scraper = new Mock<ICastingScraperStrategy>();
        scraper.Setup(s => s.IsEnabled).Returns(true);
        scraper.Setup(s => s.SourceName).Returns("TestSource");
        scraper.Setup(s => s.Region).Returns(SourceRegion.Italy);
        var call = MakeCall();
        scraper.Setup(s => s.ScrapeAsync(It.IsAny<ScraperFilter>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([call]);

        var repo = new Mock<ICastingRepository>();
        repo.Setup(r => r.ExistsByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sourceRepo = new Mock<ISourceRepository>();
        sourceRepo.Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Source?)null);

        var notifications = new Mock<INotificationService>();

        var handler = new ScrapeAllSourcesHandler(
            [scraper.Object], repo.Object, sourceRepo.Object,
            notifications.Object, NullLogger<ScrapeAllSourcesHandler>.Instance);

        var result = await handler.HandleAsync(ScraperFilter.Default);

        Assert.Equal(1, result.TotalFound);
        Assert.Equal(1, result.TotalNew);
        repo.Verify(r => r.AddRangeAsync(It.Is<IEnumerable<CastingCall>>(c => c.Contains(call)), It.IsAny<CancellationToken>()), Times.Once);
        notifications.Verify(n => n.SendAsync(call, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DuplicateCall_NotSaved()
    {
        var scraper = new Mock<ICastingScraperStrategy>();
        scraper.Setup(s => s.IsEnabled).Returns(true);
        scraper.Setup(s => s.SourceName).Returns("TestSource");
        scraper.Setup(s => s.Region).Returns(SourceRegion.Italy);
        var call = MakeCall();
        scraper.Setup(s => s.ScrapeAsync(It.IsAny<ScraperFilter>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([call]);

        var repo = new Mock<ICastingRepository>();
        repo.Setup(r => r.ExistsByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Already exists

        var sourceRepo = new Mock<ISourceRepository>();
        sourceRepo.Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Source?)null);

        var notifications = new Mock<INotificationService>();

        var handler = new ScrapeAllSourcesHandler(
            [scraper.Object], repo.Object, sourceRepo.Object,
            notifications.Object, NullLogger<ScrapeAllSourcesHandler>.Instance);

        var result = await handler.HandleAsync(ScraperFilter.Default);

        Assert.Equal(1, result.TotalFound);
        Assert.Equal(0, result.TotalNew);
        repo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<CastingCall>>(), It.IsAny<CancellationToken>()), Times.Never);
        notifications.Verify(n => n.SendAsync(It.IsAny<CastingCall>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_DisabledScraper_Skipped()
    {
        var scraper = new Mock<ICastingScraperStrategy>();
        scraper.Setup(s => s.IsEnabled).Returns(false);

        var repo = new Mock<ICastingRepository>();
        var sourceRepo = new Mock<ISourceRepository>();
        var notifications = new Mock<INotificationService>();

        var handler = new ScrapeAllSourcesHandler(
            [scraper.Object], repo.Object, sourceRepo.Object,
            notifications.Object, NullLogger<ScrapeAllSourcesHandler>.Instance);

        var result = await handler.HandleAsync(ScraperFilter.Default);

        Assert.Equal(0, result.TotalFound);
        scraper.Verify(s => s.ScrapeAsync(It.IsAny<ScraperFilter>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NotificationsRateLimited_Max10Sent()
    {
        var calls = Enumerable.Range(1, 15)
            .Select(i => MakeCall($"Call {i}", $"https://example.com/{i}"))
            .ToList();

        var scraper = new Mock<ICastingScraperStrategy>();
        scraper.Setup(s => s.IsEnabled).Returns(true);
        scraper.Setup(s => s.SourceName).Returns("TestSource");
        scraper.Setup(s => s.Region).Returns(SourceRegion.Italy);
        scraper.Setup(s => s.ScrapeAsync(It.IsAny<ScraperFilter>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(calls);

        var repo = new Mock<ICastingRepository>();
        repo.Setup(r => r.ExistsByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sourceRepo = new Mock<ISourceRepository>();
        sourceRepo.Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Source?)null);

        var notifications = new Mock<INotificationService>();

        var handler = new ScrapeAllSourcesHandler(
            [scraper.Object], repo.Object, sourceRepo.Object,
            notifications.Object, NullLogger<ScrapeAllSourcesHandler>.Instance);

        await handler.HandleAsync(ScraperFilter.Default);

        notifications.Verify(n => n.SendAsync(It.IsAny<CastingCall>(), It.IsAny<CancellationToken>()), Times.Exactly(10));
    }
}
