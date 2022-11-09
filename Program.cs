using Imageflow.Server;
using Imageflow.Server.HybridCache;
using ImageServer;
using NLog;
using NLog.Web;
using Microsoft.Extensions.Logging;

var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Info("Image Server is starting....");

try { 

    var builder = WebApplication.CreateBuilder(args);
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    logger.Debug("Image Server is reading configuration options....");

    ImageFlowConfigurationOption imageFlowConfOptions = new ImageFlowConfigurationOption();
    builder.Configuration.GetSection(ImageFlowConfigurationOption.ImageFlow).Bind(imageFlowConfOptions);


    var homeFolder = imageFlowConfOptions.CacheDirectory;
    var cacheSize = imageFlowConfOptions.CacheSize;
    builder.Services.AddImageflowHybridCache(
                    new HybridCacheOptions(Path.Combine(homeFolder, "cache"))
                    {
                        // How long after a file is created before it can be deleted
                        MinAgeToDelete = TimeSpan.FromSeconds(10),
                        // How much RAM to use for the write queue before switching to synchronous writes
                        QueueSizeLimitInBytes = 100 * 1000 * 1000,
                        // The maximum size of the cache
                        CacheSizeLimitInBytes = cacheSize,
                    }) ;
    logger.Debug("Cache folder: {0}\\cache", homeFolder);
    logger.Debug("Cache size: {0}", cacheSize);

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }
    string cacheMaxAge = imageFlowConfOptions.CacheMaxAge.Length > 0 ? imageFlowConfOptions.CacheMaxAge : "public, max-age=2592000";
    ImageflowMiddlewareOptions opts = new ImageflowMiddlewareOptions();
    logger.Debug("Max age: {0}", cacheMaxAge);

    opts.SetMapWebRoot(true)
        .SetMyOpenSourceProjectUrl("https://github.com/engitel/image-server")
        .SetAllowCaching(true)
        .SetDefaultCacheControlString(cacheMaxAge)
        // defaults
        .AddCommandDefault("down.filter", "mitchell")
        .AddCommandDefault("f.sharpen", "15")
        .AddCommandDefault("webp.quality", "90")
        .AddCommandDefault("ignore_icc_errors", "true");
    
    if (imageFlowConfOptions.DiagnosticPassword.Length > 0)
    {
        opts.SetDiagnosticsPageAccess(app.Environment.IsDevelopment() ? AccessDiagnosticsFrom.AnyHost : AccessDiagnosticsFrom.LocalHost)
        .SetDiagnosticsPagePassword(imageFlowConfOptions.DiagnosticPassword);
        logger.Debug("DiagnosticPassword is set");
    }

    if (imageFlowConfOptions.SignatureKey.Length > 0)
    {    
        opts.SetRequestSignatureOptions(
            new RequestSignatureOptions(SignatureRequired.ForQuerystringRequests, new[] { imageFlowConfOptions.SignatureKey })
        ).SetUsePresetsExclusively(false);
        logger.Debug("Accepts sigend query-string requests");
    } else
    {
        opts.SetUsePresetsExclusively(true);
        logger.Debug("Accepts only presets requests");
    }

    // creates presets
    foreach (PresetConfigurationOption item in imageFlowConfOptions.Presets)
    {
        PresetOptions opt = new PresetOptions(item.Name, PresetPriority.DefaultValues);
        logger.Debug("Add Preset {0}", item.Name);

        foreach (var com in item.Commands)
        {
            opt.SetCommand(com.Name, com.Value);
        }
        opts.AddPreset(opt);
    }

    // Imageflow
    app.UseImageflow(opts);
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.Run();
}
catch (Exception exception)
{
    // NLog: catch setup errors
    logger.Error("Image Server Exception {0}", exception.Message);
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    NLog.LogManager.Shutdown();
}
