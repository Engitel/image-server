using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ImageServer.Authorization;

public static class SgAuthorizeServiceCollectionExtensions
{
    /// <summary>
    /// Registers SGAuthorize services when <c>SGAuthorize:Filter</c> is set.
    /// With no filter, registration is a no-op so requests are never evaluated.
    /// </summary>
    public static IServiceCollection AddSgAuthorize(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(SgAuthorizeOptions.Section);
        services.Configure<SgAuthorizeOptions>(section);

        var options = section.Get<SgAuthorizeOptions>() ?? new SgAuthorizeOptions();
        if (!options.IsEnabled)
        {
            return services;
        }

        services.AddMemoryCache();
        services.TryAddSingleton<ILegacyFormsAuthTicketReader, LegacyFormsAuthTicketReader>();
        services.TryAddSingleton<IAuthorizationCheckService, AuthorizationCheckService>();
        return services;
    }

    /// <summary>
    /// Adds request authorization middleware only when a filter is configured.
    /// </summary>
    public static IApplicationBuilder UseSgAuthorize(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetService<IOptions<SgAuthorizeOptions>>()?.Value;
        if (options is null || !options.IsEnabled)
        {
            return app;
        }

        return app.UseMiddleware<RequestAuthorizeMiddleware>();
    }
}
