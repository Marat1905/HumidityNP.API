using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Humidity.API.Auth;


public class DynamicAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly IConfiguration _configuration;
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public DynamicAuthorizationPolicyProvider(
        IConfiguration configuration,
        IOptions<AuthorizationOptions> options)
    {
        _configuration = configuration;
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
    {
        var roles = _configuration
            .GetSection($"AuthorizationPolicies:{policyName}")
            .Get<string[]>();

        if (roles != null && roles.Length > 0)
        {
            var builder = new AuthorizationPolicyBuilder();

            builder.RequireRole(roles);

            return Task.FromResult(builder.Build());
        }

        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _fallbackPolicyProvider.GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _fallbackPolicyProvider.GetFallbackPolicyAsync();
    }
}