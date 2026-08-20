using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using WikiScrapper.Data;
using WikiScrapper.Localization;
using WikiScrapper.Logging;
using WikiScrapper.Middleware;

// Bootstrap logger so that startup failures are captured before the host is built.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting WikiScrapper web host");

    var builder = WebApplication.CreateBuilder(args);

    // Serilog replaces the default logging pipeline; sinks are configured in appsettings.json.
    builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddWikiScrapperServices(builder.Configuration);

    builder.Services.AddLocalization();

    builder.Services.AddControllersWithViews()
        .AddViewLocalization();

    var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("pl") };
    builder.Services.Configure<RequestLocalizationOptions>(options =>
    {
        options.DefaultRequestCulture = new RequestCulture("en");
        options.SupportedCultures = supportedCultures;
        options.SupportedUICultures = supportedCultures;
        options.RequestCultureProviders.Insert(0, new WikiCultureProvider());
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "WikiScrapper API",
            Version = "v1",
            Description = "REST API for Wikipedia descriptions of Polish voivodeships and world countries.",
        });

        var xmlPath = Path.Combine(AppContext.BaseDirectory, "WikiScrapper.xml");
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });

    var app = builder.Build();

    // Apply migrations and seed reference data so the app is usable right after `dotnet run`.
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
        await SeedData.SeedAsync(dbContext);
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

    app.UseHttpsRedirection();
    app.UseRequestLocalization();
    app.UseRouting();
    app.UseAuthorization();

    // After routing so static-asset endpoints still run; noisy paths are filtered in the logger.
    app.UseTimestampedRequestLogging();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "WikiScrapper API v1");
    });

    app.MapStaticAssets();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // HostAbortedException is thrown by design when EF Core tooling builds the host at design time.
    Log.Fatal(ex, "WikiScrapper web host terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
