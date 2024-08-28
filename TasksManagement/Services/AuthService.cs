using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TasksManagement.API.Contracts;
using TasksManagement.API.Models;
using TasksManagement.API.Models.Entities;

namespace TasksManagement.API.Services;

public record RefreshRequest(
    string AccessToken,
    string RefreshToken);

public record RegisterRequest(
    string Names,
    string LastNames,
    string Email,
    string Password,
    Guid RoleId);

public record AuthResponse(
    string AccessToken,
    long ExpiresIn,
    DateTime ExpirationDate,
    string RefreshToken);

public record AuthRequest(
    string Email, 
    string Password);

public class AuthService(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IOptions<JwtConfiguration> options) : IAuthService
{
    private readonly JwtConfiguration jwtConfiguration = options.Value;

    public async Task<AuthResponse> Login(AuthRequest request)
    {
        var user = await userManager.FindByNameAsync(request.Email);
        var isValidPassword = await userManager.CheckPasswordAsync(user, request.Password);
        if (user == null || !isValidPassword)
            throw new UnauthorizedAccessException("Invalid credentials");

        var refreshToken = await GenerateRefreshToken(user);

        var userRoles = await userManager.GetRolesAsync(user);

        var token = await GenerateAccessToken(user);

        return new AuthResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            jwtConfiguration.TokenValidityInMinutes,
            token.ValidTo,
            refreshToken
            );
    }

    public async Task<AuthResponse> Refresh(RefreshRequest request)
    {
        string? accessToken = request.AccessToken;
        string? refreshToken = request.RefreshToken;

        var principal = GetPrincipalFromToken(accessToken, false);
        Guard.Against.Null(principal, message: "Invalid access token or refresh token");

        string username = principal.Identity.Name;

        var user = await userManager.FindByNameAsync(username);
        Guard.Against.Null(user, message: "Invalid access");

        var isInvalidRefreshToken = user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now;

        if (isInvalidRefreshToken)
            throw new BadHttpRequestException("Invalid refresh token");

        var userRoles = await userManager.GetRolesAsync(user);

        var newAccessToken = CreateJWTToken(principal.Claims.ToList());
        var newRefreshToken = CreateRefreshToken();

        user.RefreshToken = newRefreshToken;
        await userManager.UpdateAsync(user);

        var writtenToken = new JwtSecurityTokenHandler().WriteToken(newAccessToken);

        return new AuthResponse(
            writtenToken,
            jwtConfiguration.TokenValidityInMinutes,
            newAccessToken.ValidTo,
            newRefreshToken
            );
    }

    public async Task<AuthResponse> Register(RegisterRequest request)
    {
        var user = await CreateUser(request);

        var refreshToken = await GenerateRefreshToken(user);

        var token = await GenerateAccessToken(user);

        return new AuthResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            jwtConfiguration.TokenValidityInMinutes,
            token.ValidTo,
            refreshToken
            );
    }

    private async Task<User> CreateUser(RegisterRequest request)
    {
        var userExists = await userManager.FindByNameAsync(request.Email);
        if (userExists != null) throw new BadHttpRequestException("User is already created");

        User user = new(request.Names, request.LastNames)
        {
            UserName = request.Email,
            Email = request.Email
        };
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new BadHttpRequestException($"Failed to create user: {string.Join(", ", result.Errors.Select(x => x.Description).ToArray())}");

        var role = await roleManager.FindByIdAsync(request.RoleId.ToString());
        Guard.Against.Null(role, message: $"RoleId with id {request.RoleId} was not found");
        await userManager.AddToRoleAsync(user, role.Name);

        return user;
    }

    private async Task<JwtSecurityToken> GenerateAccessToken(User user)
    {
        var userRoles = await userManager.GetRolesAsync(user);

        var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iss, jwtConfiguration.ValidIssuer),
            new Claim(JwtRegisteredClaimNames.Aud, jwtConfiguration.ValidAudience),
        };

        foreach (var userRole in userRoles)
        {
            authClaims.Add(new Claim(ClaimTypes.Role, userRole));
        }

        var token = CreateJWTToken(authClaims);

        return token;
    }

    private JwtSecurityToken CreateJWTToken(List<Claim> authClaims)
    {
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfiguration.Secret));

        var token = new JwtSecurityToken(
            expires: DateTime.Now.AddMinutes(jwtConfiguration.TokenValidityInMinutes),
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

        return token;
    }

    public async Task<string> GenerateRefreshToken(User user)
    {
        var refreshToken = CreateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.Now.AddDays(jwtConfiguration.RefreshTokenValidityInDays);
        await userManager.UpdateAsync(user);

        return refreshToken;
    }

    private static string CreateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal? GetPrincipalFromToken(string? token, bool validateLifeTime = true)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfiguration.Secret)),
            ValidAudience = jwtConfiguration.ValidAudience,
            ValidIssuer = jwtConfiguration.ValidIssuer,
            ValidateLifetime = validateLifeTime
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
        if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        return principal;
    }
}
