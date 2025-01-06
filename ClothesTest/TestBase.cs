using AutoFixture;
using AutoMapper;
using Clothes.Controllers;
using Clothes.JwtFeatures;
using Clothes.Models.Entities;
using EmailService;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace ClothesTest;

public abstract class TestBase
{
    protected readonly IFixture Fixture;
    protected readonly UserManager<User> UserManagerMock;
    protected readonly IMapper MapperMock;
    protected readonly JwtHandler JwtHandler;
    protected readonly IEmailSender EmailSenderMock;
    protected readonly AccountsController AccountsController;
    protected const string RefreshToken = "TestRefreshToken";
    protected const string AnotherToken = "AnotherToken";
    protected const string SecurityKey = "YourSecretKeyForTestingTESTING12";

    protected TestBase()
    {
        Fixture = new Fixture();
        Fixture.Behaviors.Add(new OmitOnRecursionBehavior()); // Avoid recursion issues in AutoFixture

        UserManagerMock = Substitute.For<UserManager<User>>(
            Substitute.For<IUserStore<User>>(), null, null, null, null, null, null, null, null);
        MapperMock = Substitute.For<IMapper>();
        EmailSenderMock = Substitute.For<IEmailSender>();

        var mockConfiguration = Substitute.For<IConfiguration>();

        // Mock the "JWTSettings" section of IConfiguration
        var jwtSettingsSection = Substitute.For<IConfigurationSection>();
        jwtSettingsSection["securityKey"].Returns(SecurityKey);
        jwtSettingsSection["validIssuer"].Returns("https://valid-issuer.com");
        jwtSettingsSection["validAudience"].Returns("https://valid-audience.com");
        jwtSettingsSection["expiryMinutes"].Returns("30");

        mockConfiguration.GetSection("JWTSettings").Returns(jwtSettingsSection);

        JwtHandler = new JwtHandler(mockConfiguration);

        AccountsController = new AccountsController(
            UserManagerMock, MapperMock, JwtHandler, EmailSenderMock);
    }
}