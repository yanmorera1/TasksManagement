using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using TasksManagement.API.Contracts;

namespace TasksManagement.API.Common.Schemas;
public class CustomAuthenticationScheme : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string CustomScheme = nameof(CustomAuthenticationScheme);
    private readonly IAuthService _authService;

    public CustomAuthenticationScheme(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IServiceProvider serviceProvider
        ) : base(options, logger, encoder)
    {
        var _scope = serviceProvider.CreateScope();
        _authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Context.Request.Query.TryGetValue("accessToken", out StringValues accessToken))
            return Task.FromResult(AuthenticateResult.Fail("access token not found"));

        var claims = _authService.GetPrincipalFromToken(accessToken);
        if (claims == null)
            return Task.FromResult(AuthenticateResult.Fail("token invalid"));

        var identity = new ClaimsIdentity(claims.Claims, CustomScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, new(), CustomScheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
