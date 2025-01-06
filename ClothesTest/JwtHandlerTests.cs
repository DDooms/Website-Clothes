using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Clothes.Models.Entities;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;

namespace ClothesTest;

public class JwtHandlerTests : TestBase
{
    [Fact]
    public void CreateTokenWithRefreshToken_ShouldReturnValidTokens()
    {
        // Arrange
        var user = new User
        {
            Id = "123",
            UserName = "testUser",
            FirstName = "Test",
            LastName = "User"
        };
        var roles = new List<string> { "Admin", "User" };

        // Act
        var (accessToken, refreshToken) = JwtHandler.CreateTokenWithRefreshToken(user, roles);

        // Assert
        accessToken.Should().NotBeNullOrEmpty();
        refreshToken.Should().NotBeNullOrEmpty();

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(accessToken);

        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == "testUser");
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "123");
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "User");
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ShouldReturnPrincipal_WhenTokenIsExpired()
    {
        // Arrange
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecurityKey)),
            SecurityAlgorithms.HmacSha256Signature);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "testUser"),
            new(ClaimTypes.NameIdentifier, "123")
        };

        var tokenOptions = new JwtSecurityToken(
            issuer: "https://valid-issuer.com",
            audience: "https://valid-audience.com",
            claims: claims,
            expires: DateTime.Now.AddSeconds(-1), // Token is expired
            signingCredentials: signingCredentials);

        var expiredToken = new JwtSecurityTokenHandler().WriteToken(tokenOptions);

        // Act
        var principal = JwtHandler.GetPrincipalFromExpiredToken(expiredToken);

        // Assert
        principal.Should().NotBeNull();
        principal.Identity!.Name.Should().Be("testUser");
    }

    [Fact]
    public void HashRefreshToken_ShouldReturnConsistentHash()
    {
        // Act
        var hashedToken = JwtHandler.HashRefreshToken(RefreshToken);

        // Assert
        hashedToken.Should().NotBeNullOrEmpty();
        hashedToken.Should().Be(JwtHandler.HashRefreshToken(RefreshToken)); // Consistency
    }

    [Fact]
    public void VerifyRefreshToken_ShouldReturnTrue_ForMatchingTokens()
    {
        var hashedToken = JwtHandler.HashRefreshToken(RefreshToken);

        // Act
        var result = JwtHandler.VerifyRefreshToken(hashedToken, RefreshToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyRefreshToken_ShouldReturnFalse_ForNonMatchingTokens()
    {
        var hashedToken = JwtHandler.HashRefreshToken(RefreshToken);

        // Act
        var result = JwtHandler.VerifyRefreshToken(hashedToken, AnotherToken);

        // Assert
        result.Should().BeFalse();
    }
}