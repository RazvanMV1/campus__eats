using CampusEats.Backend.Common.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CampusEats.Tests.Common.Services;

public class JwtServiceTests
{
    private JwtService CreateJwtService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:SecretKey", "ThisIsAVerySecureSecretKeyForTestingPurposesOnly12345" },
                { "Jwt:Issuer", "TestIssuer" },
                { "Jwt:Audience", "TestAudience" },
                { "Jwt:ExpirationDays", "7" }
            })
            .Build();

        return new JwtService(configuration);
    }

    [Fact]
    public void GenerateToken_CreatesValidToken()
    {
        // Arrange
        var jwtService = CreateJwtService();
        var userId = Guid.NewGuid();

        // Act
        var token = jwtService.GenerateToken(userId, "test@campus.ro", "Student", "Test User");

        // Assert
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateToken_ContainsCorrectClaims()
    {
        // Arrange
        var jwtService = CreateJwtService();
        var userId = Guid.NewGuid();
        var email = "test@campus.ro";
        var role = "Student";
        var fullName = "Test User";

        // Act
        var token = jwtService.GenerateToken(userId, email, role, fullName);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == email);
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == fullName);
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == role);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateToken_HasCorrectIssuerAndAudience()
    {
        // Arrange
        var jwtService = CreateJwtService();
        var userId = Guid.NewGuid();

        // Act
        var token = jwtService.GenerateToken(userId, "test@campus.ro", "Student", "Test User");

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be("TestIssuer");
        jwtToken.Audiences.Should().Contain("TestAudience");
    }

    [Fact]
    public void GenerateToken_HasCorrectExpiration()
    {
        // Arrange
        var jwtService = CreateJwtService();
        var userId = Guid.NewGuid();

        // Act
        var token = jwtService.GenerateToken(userId, "test@campus.ro", "Student", "Test User");

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.ValidTo.Should().BeAfter(DateTime.UtcNow);
        jwtToken.ValidTo.Should().BeBefore(DateTime.UtcNow.AddDays(8)); // Should expire within 8 days (7 days config)
    }

    [Fact]
    public void ValidateToken_WithValidToken_ReturnsPrincipal()
    {
        // Arrange
        var jwtService = CreateJwtService();
        var userId = Guid.NewGuid();
        var token = jwtService.GenerateToken(userId, "test@campus.ro", "Student", "Test User");

        // Act
        var principal = jwtService.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == "Test User");
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var jwtService = CreateJwtService();
        var invalidToken = "invalid.token.string";

        // Act
        var principal = jwtService.ValidateToken(invalidToken);

        // Assert
        principal.Should().BeNull();
    }
}
