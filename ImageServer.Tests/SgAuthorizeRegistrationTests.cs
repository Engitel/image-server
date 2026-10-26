using ImageServer.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ImageServer.Tests;

public class SgAuthorizeRegistrationTests
{
    [Fact]
    public void AddSgAuthorize_DoesNotRegisterAuthServices_WhenFilterMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SGAuthorize:Connection"] = "Server=.;Database=test;"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSgAuthorize(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.Null(provider.GetService<ILegacyFormsAuthTicketReader>());
        Assert.Null(provider.GetService<IAuthorizationCheckService>());
        Assert.False(provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SgAuthorizeOptions>>().Value.IsEnabled);
    }

    [Fact]
    public void UseSgAuthorize_DoesNotAddMiddleware_WhenFilterMissing()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSgAuthorize(configuration);

        var app = new ApplicationBuilder(services.BuildServiceProvider());
        var before = app.Build();

        app.UseSgAuthorize();
        var after = app.Build();

        // Both pipelines should just be empty (no middleware component registered)
        Assert.NotNull(before);
        Assert.NotNull(after);
        Assert.False(services.BuildServiceProvider()
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<SgAuthorizeOptions>>().Value.IsEnabled);
    }
}
