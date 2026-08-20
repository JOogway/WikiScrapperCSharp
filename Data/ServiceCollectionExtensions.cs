using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using WikiScrapper.Data.Repositories;
using WikiScrapper.Domain.Interfaces;
using WikiScrapper.Services;

namespace WikiScrapper.Data;

/// <summary>
/// Composition-root extensions for registering persistence, Wikipedia, and sync services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the DbContext (SQL Server / LocalDB), repositories, the resilient
    /// Wikipedia HTTP clients, and background sync services.
    /// </summary>
    public static IServiceCollection AddWikiScrapperServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.EnableRetryOnFailure(maxRetryCount: 3)));

        services.Configure<WikipediaClientOptions>(
            configuration.GetSection(WikipediaClientOptions.SectionName));

        services.AddHttpContextAccessor();
        services.AddScoped<IWikiLanguageAccessor, WikiLanguageAccessor>();

        services.AddScoped<IVoivodeshipRepository, VoivodeshipRepository>();
        services.AddScoped<ICountryRepository, CountryRepository>();
        services.AddScoped<IAppLogRepository, AppLogRepository>();
        services.AddScoped<ISyncDbBatch, SyncDbBatch>();

        RegisterWikipediaClient(services, WikipediaClientOptions.EnClientName, options => options.EnBaseUrl);
        RegisterWikipediaClient(services, WikipediaClientOptions.PlClientName, options => options.PlBaseUrl);
        services.AddScoped<IWikipediaService, WikipediaService>();

        services.AddScoped<IDataSyncService, DataSyncService>();
        services.AddSingleton<SyncJobService>();
        services.AddSingleton<ISyncJobService>(sp => sp.GetRequiredService<SyncJobService>());
        services.AddSingleton<ISyncProgress>(sp => sp.GetRequiredService<SyncJobService>());

        return services;
    }

    private static void RegisterWikipediaClient(
        IServiceCollection services,
        string clientName,
        Func<WikipediaClientOptions, string> baseUrlSelector)
    {
        services.AddHttpClient(clientName, (sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<WikipediaClientOptions>>().Value;
                client.BaseAddress = new Uri(baseUrlSelector(options));
                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "WikiScrapper/1.0 (recruitment prototype; contact: local)");
                client.Timeout = WikipediaResilience.HttpClientTimeout;
            })
            .AddResilienceHandler(clientName, (builder, context) =>
            {
                var logger = context.ServiceProvider
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger($"WikiScrapper.Wikipedia.Resilience.{clientName}");
                WikipediaResilience.Configure(builder, logger);
            });
    }
}
