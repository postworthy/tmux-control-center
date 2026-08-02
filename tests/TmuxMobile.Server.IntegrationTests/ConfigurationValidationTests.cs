using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using TmuxMobile.Core;
using TmuxMobile.Server;

namespace TmuxMobile.Server.IntegrationTests;

public sealed class ConfigurationValidationTests
{
    [Fact]
    public void RejectsAccidentalProductionBypass()
    {
        var validator = Validator("Production");
        var result = validator.Validate(null, new AuthOptions
        {
            Mode = "Disabled",
            UnsafeAllowProductionBypass = true
        });
        Assert.True(result.Failed);
    }

    [Fact]
    public async Task ProductionHostCannotStartWithDisabledAuthenticationOverride()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["AllowedHosts"] = "localhost",
                    ["Urls"] = "https://127.0.0.1:5443",
                    ["Authentication:Mode"] = "Disabled",
                    ["Authentication:UnsafeAllowProductionBypass"] = "true",
                    ["Security:AllowedOrigins:0"] = "https://localhost",
                    ["Audit:Destination"] = Path.Combine(Path.GetTempPath(), "tmux-mobile-invalid-auth-audit.jsonl"),
                    ["DataProtection:KeysDirectory"] = Path.Combine(Path.GetTempPath(), "tmux-mobile-invalid-auth-keys")
                }));
        });

        var exception = await Assert.ThrowsAnyAsync<Exception>(() => factory.CreateClient().GetAsync("/health/live"));
        Assert.Contains("production authentication cannot be disabled", exception.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RejectsWildcardOriginsAndRelativeTmuxPath()
    {
        var validator = Validator("Production");
        Assert.True(validator.Validate(null, new SecurityOptions { AllowedOrigins = ["*"] }).Failed);
        Assert.True(validator.Validate(null, new TmuxOptions { ExecutablePath = "tmux" }).Failed);
    }

    [Fact]
    public void InsecureHttpCookiesRequireApiKeyAuthentication()
    {
        var validator = Validator("Production");

        Assert.True(validator.Validate(null, new AuthOptions
        {
            Mode = "Disabled",
            UnsafeAllowProductionBypass = true,
            UnsafeAllowInsecureHttp = true
        }).Failed);

        Assert.False(validator.Validate(null, new AuthOptions
        {
            Mode = "ApiKey",
            ApiKey = new string('x', 32),
            UnsafeAllowInsecureHttp = true,
            UnsafeTestProfileAcknowledgement = AuthOptions.TailnetTestAcknowledgement
        }).Failed);
    }

    [Fact]
    public void WeakApiKeyOverrideRequiresApiKeyModeAndEightCharacters()
    {
        var validator = Validator("Production");

        Assert.True(validator.Validate(null, new AuthOptions
        {
            Mode = "ApiKey",
            ApiKey = "test-key",
            UnsafeAllowWeakApiKeyForTest = true,
            UnsafeTestProfileAcknowledgement = AuthOptions.TailnetTestAcknowledgement
        }).Succeeded);

        Assert.True(validator.Validate(null, new AuthOptions
        {
            Mode = "ApiKey",
            ApiKey = "short",
            UnsafeAllowWeakApiKeyForTest = true,
            UnsafeTestProfileAcknowledgement = AuthOptions.TailnetTestAcknowledgement
        }).Failed);

        Assert.True(validator.Validate(null, new AuthOptions
        {
            Mode = "Disabled",
            UnsafeAllowProductionBypass = true,
            UnsafeAllowWeakApiKeyForTest = true
        }).Failed);

        Assert.True(validator.Validate(null, new AuthOptions
        {
            Mode = "ApiKey",
            ApiKey = "test-key",
            UnsafeAllowInsecureHttp = true,
            UnsafeAllowWeakApiKeyForTest = true,
            UnsafeTestProfileAcknowledgement = AuthOptions.TailnetTestAcknowledgement
        }).Succeeded);

        Assert.True(validator.Validate(null, new AuthOptions
        {
            Mode = "ApiKey",
            ApiKey = "test-key",
            UnsafeAllowWeakApiKeyForTest = true
        }).Failed);
    }

    [Fact]
    public void ShortApiKeyRemainsRejectedByDefault()
    {
        var validator = Validator("Production");

        Assert.True(validator.Validate(null, new AuthOptions
        {
            Mode = "ApiKey",
            ApiKey = "test-key"
        }).Failed);
    }

    [Theory]
    [InlineData("*")]
    [InlineData("https://tmux.example/path")]
    [InlineData("https://user@tmux.example")]
    [InlineData("https://tmux.example?query=1")]
    [InlineData("http://tmux.example")]
    public void RejectsUnsafeProductionOrigins(string origin)
    {
        var validator = Validator("Production", new Dictionary<string, string?>
        {
            ["AllowedHosts"] = "tmux.example",
            ["Urls"] = "http://127.0.0.1:5179",
            ["Authentication:Mode"] = "ApiKey",
            ["Authentication:ApiKey"] = new string('x', 32)
        });
        Assert.True(validator.Validate(null, new SecurityOptions
        {
            AllowedOrigins = [origin],
            ExternalHttpsTermination = true
        }).Failed);
    }

    [Fact]
    public void AcceptsExactHttpsOriginBehindTerminator()
    {
        var validator = Validator("Production", new Dictionary<string, string?>
        {
            ["AllowedHosts"] = "tmux.example",
            ["Urls"] = "http://127.0.0.1:5179",
            ["Authentication:Mode"] = "ApiKey",
            ["Authentication:ApiKey"] = new string('x', 32)
        });
        Assert.True(validator.Validate(null, new SecurityOptions
        {
            AllowedOrigins = ["https://tmux.example"],
            ExternalHttpsTermination = true
        }).Succeeded);
    }

    [Fact]
    public void RejectsOriginHostAbsentFromAllowedHosts()
    {
        var validator = Validator("Production", new Dictionary<string, string?>
        {
            ["AllowedHosts"] = "other.example",
            ["Urls"] = "https://127.0.0.1:5443",
            ["Authentication:Mode"] = "ApiKey",
            ["Authentication:ApiKey"] = new string('x', 32)
        });
        Assert.True(validator.Validate(null, new SecurityOptions
        {
            AllowedOrigins = ["https://tmux.example"]
        }).Failed);
    }

    [Fact]
    public void ForwardedHeadersRequireExplicitValidProxy()
    {
        var validator = Validator("Production");
        Assert.True(validator.Validate(null, new ForwardedHeaderSettings
        {
            Enabled = true,
            KnownProxies = []
        }).Failed);
        Assert.True(validator.Validate(null, new ForwardedHeaderSettings
        {
            Enabled = true,
            KnownProxies = ["not-an-ip"]
        }).Failed);
    }

    private static SecurityConfigurationValidator Validator(string environment,
        IDictionary<string, string?>? values = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values ?? new Dictionary<string, string?>
            {
                ["AllowedHosts"] = "localhost",
                ["Urls"] = "https://127.0.0.1:5443",
                ["Authentication:Mode"] = "ApiKey",
                ["Authentication:ApiKey"] = new string('x', 32)
            }).Build();
        return new SecurityConfigurationValidator(new EnvironmentStub(environment), configuration);
    }

    private sealed class EnvironmentStub(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
