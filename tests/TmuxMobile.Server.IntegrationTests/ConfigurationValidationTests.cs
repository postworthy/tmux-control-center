using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TmuxMobile.Core;
using TmuxMobile.Server;

namespace TmuxMobile.Server.IntegrationTests;

public sealed class ConfigurationValidationTests
{
    [Fact]
    public void RejectsAccidentalProductionBypass()
    {
        var validator = new SecurityConfigurationValidator(new EnvironmentStub("Production"));
        var result = validator.Validate(null, new AuthOptions
        {
            Mode = "Disabled",
            UnsafeAllowProductionBypass = false
        });
        Assert.True(result.Failed);
    }

    [Fact]
    public void RejectsWildcardOriginsAndRelativeTmuxPath()
    {
        var validator = new SecurityConfigurationValidator(new EnvironmentStub("Production"));
        Assert.True(validator.Validate(null, new SecurityOptions { AllowedOrigins = ["*"] }).Failed);
        Assert.True(validator.Validate(null, new TmuxOptions { ExecutablePath = "tmux" }).Failed);
    }

    [Fact]
    public void InsecureHttpCookiesRequireApiKeyAuthentication()
    {
        var validator = new SecurityConfigurationValidator(new EnvironmentStub("Production"));

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
            UnsafeAllowInsecureHttp = true
        }).Failed);
    }

    [Fact]
    public void WeakApiKeyOverrideRequiresApiKeyModeAndEightCharacters()
    {
        var validator = new SecurityConfigurationValidator(new EnvironmentStub("Production"));

        Assert.True(validator.Validate(null, new AuthOptions
        {
            Mode = "ApiKey",
            ApiKey = "test-key",
            UnsafeAllowWeakApiKeyForTest = true
        }).Succeeded);

        Assert.True(validator.Validate(null, new AuthOptions
        {
            Mode = "ApiKey",
            ApiKey = "short",
            UnsafeAllowWeakApiKeyForTest = true
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
            UnsafeAllowWeakApiKeyForTest = true
        }).Succeeded);
    }

    [Fact]
    public void ShortApiKeyRemainsRejectedByDefault()
    {
        var validator = new SecurityConfigurationValidator(new EnvironmentStub("Production"));

        Assert.True(validator.Validate(null, new AuthOptions
        {
            Mode = "ApiKey",
            ApiKey = "test-key"
        }).Failed);
    }

    private sealed class EnvironmentStub(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
