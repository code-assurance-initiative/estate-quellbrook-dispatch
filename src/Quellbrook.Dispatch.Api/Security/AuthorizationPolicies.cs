using Microsoft.AspNetCore.Authorization;

namespace Quellbrook.Dispatch.Api.Security;

/// <summary>The named policies: each requires one OAuth scope. The gateway is the only client holding them.</summary>
public static class AuthorizationPolicies
{
    public const string ReadDispatch = "dispatch:read";
    public const string WriteDispatch = "dispatch:write";
    public const string AdministerFleet = "fleet:admin";

    public static AuthorizationBuilder AddDispatchPolicies(this AuthorizationBuilder builder) =>
        builder
            .AddPolicy(ReadDispatch, policy => policy.RequireScope(ReadDispatch))
            .AddPolicy(WriteDispatch, policy => policy.RequireScope(WriteDispatch))
            .AddPolicy(AdministerFleet, policy => policy.RequireScope(AdministerFleet));

    private static AuthorizationPolicyBuilder RequireScope(this AuthorizationPolicyBuilder policy, string scope) =>
        policy
            .RequireAuthenticatedUser()
            .RequireAssertion(context => ScopeClaims.HasScope(context.User, scope));
}
