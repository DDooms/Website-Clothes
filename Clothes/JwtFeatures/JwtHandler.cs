using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Clothes.Models.Entities;
using Microsoft.IdentityModel.Tokens;

namespace Clothes.JwtFeatures;

public class JwtHandler
{
    private readonly IConfiguration Configuration;
    private readonly IConfigurationSection JwtSettings;

    public JwtHandler(IConfiguration configuration)
    {
        Configuration = configuration;
        JwtSettings = Configuration.GetSection("JWTSettings");
    }

    public (string AccessToken, string RefreshToken) CreateTokenWithRefreshToken(User user, IList<string> roles)
    {
        var signingCredentials = GetSigningCredentials();
        var claims = GetClaims(user, roles);
        var tokenOptions = GenerateTokenOptions(signingCredentials, claims);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(tokenOptions);
        var refreshToken = GenerateRefreshToken();

        return (accessToken, refreshToken);
    }

    private SigningCredentials GetSigningCredentials()
    {
        var key = Encoding.UTF8.GetBytes(JwtSettings["securityKey"]);
        return new SigningCredentials(
            new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature);
    }

    private List<Claim> GetClaims(User user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.NameIdentifier, user.Id)

        };
        
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        
        return claims;
    }

    private JwtSecurityToken GenerateTokenOptions(SigningCredentials signingCredentials, List<Claim> claims)
    {
        var tokenOptions = new JwtSecurityToken(
            issuer: JwtSettings["validIssuer"],
            audience: JwtSettings["validAudience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(Convert.ToDouble(JwtSettings["expiryMinutes"])),
            signingCredentials: signingCredentials
        );
        return tokenOptions;
    }
    
    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
    
    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSettings["securityKey"])),
            ValidateLifetime = false, // Allow expired tokens only for refresh tokens
                                      // to get the information about the user
            ValidIssuer = JwtSettings["validIssuer"],
            ValidAudience = JwtSettings["validAudience"]
        };
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
        var jwtSecurityToken = securityToken as JwtSecurityToken;
        
        if (!jwtSecurityToken.Header.Alg.Equals("http://www.w3.org/2001/04/xmldsig-more#hmac-sha256", 
                StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token");
        }
        
        return principal;
    }
    
    // Hash the refresh token using SHA-256
    public string HashRefreshToken(string refreshToken)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(hashBytes);
    }

    // Verify the hashed refresh token
    public bool VerifyRefreshToken(string hashedToken, string providedToken)
    {
        var hashedProvidedToken = HashRefreshToken(providedToken);
        return hashedToken == hashedProvidedToken;
    }
}