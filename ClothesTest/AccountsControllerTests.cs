using System.Security.Claims;
using AutoFixture;
using Bogus;
using Clothes.Models.DTOs.AuthenticationDTOs;
using Clothes.Models.DTOs.PasswordDTOs;
using Clothes.Models.DTOs.RegistrationDTOs;
using Clothes.Models.Entities;
using EmailService;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace ClothesTest;

public class AccountsControllerTests : TestBase
{
    [Fact]
    public async Task Register_ValidInput_ReturnsCreatedResponse()
    {
        // Arrange
        var faker = new Faker();
        var registrationDto = new UserForRegistrationDto
        {
            Email = faker.Internet.Email(),
            Password = faker.Internet.Password(8, true),
            FirstName = faker.Name.FirstName(),
            LastName = faker.Name.LastName()
        };

        var user = Fixture.Create<User>();
        MapperMock.Map<User>(registrationDto).Returns(user);
        UserManagerMock.CreateAsync(user, registrationDto.Password).Returns(IdentityResult.Success);
        UserManagerMock.GenerateEmailConfirmationTokenAsync(user).Returns(AnotherToken);
        UserManagerMock.AddToRoleAsync(user, "Visitor").Returns(IdentityResult.Success);

        // Act
        var result = await AccountsController.Register(registrationDto);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
        var response = createdResult.Value.Should().BeOfType<RegistrationResponseDto>().Subject;
        response.IsSuccessfulRegistration.Should().BeTrue();

        await UserManagerMock.Received(1).CreateAsync(user, registrationDto.Password);
        await UserManagerMock.Received(1).GenerateEmailConfirmationTokenAsync(user);
        await EmailSenderMock.Received(1).SendEmailAsync(Arg.Any<Message>());
    }

    [Fact]
    public async Task Authenticate_ValidCredentials_ReturnsOkWithTokens()
    {
        // Arrange
        var roles = new List<string> { "Visitor" };
        var faker = new Faker();
        var authDto = new UserForAuthenticationDto
        {
            Email = faker.Internet.Email(),
            Password = faker.Internet.Password()
        };

        var user = Fixture.Build<User>()
            .With(u => u.Email, authDto.Email)
            .Create();

        UserManagerMock.FindByNameAsync(authDto.Email).Returns(user);
        UserManagerMock.IsEmailConfirmedAsync(user).Returns(true);
        UserManagerMock.CheckPasswordAsync(user, authDto.Password).Returns(true);
        UserManagerMock.GetRolesAsync(user).Returns(roles);
        
        // Act
        var result = await AccountsController.Authenticate(authDto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AuthResponseDto>().Subject;
        response.IsAuthSuccessful.Should().BeTrue();

        await UserManagerMock.Received(1).FindByNameAsync(authDto.Email);
        await UserManagerMock.Received(1).CheckPasswordAsync(user, authDto.Password);
    }

    [Fact]
    public async Task ForgotPassword_ValidEmail_SendsResetLink()
    {
        // Arrange
        var faker = new Faker();
        var forgotPasswordDto = new ForgotPasswordDto
        {
            Email = faker.Internet.Email()
        };

        var user = Fixture.Build<User>()
            .With(u => u.Email, forgotPasswordDto.Email)
            .Create();

        UserManagerMock.FindByEmailAsync(forgotPasswordDto.Email).Returns(user);
        UserManagerMock.GeneratePasswordResetTokenAsync(user).Returns(AnotherToken);

        // Act
        var result = await AccountsController.ForgotPassword(forgotPasswordDto);

        // Assert
        result.Should().BeOfType<OkResult>();

        await UserManagerMock.Received(1).FindByEmailAsync(forgotPasswordDto.Email);
        await UserManagerMock.Received(1).GeneratePasswordResetTokenAsync(user);
        await EmailSenderMock.Received(1).SendEmailAsync(Arg.Any<Message>());
    }

    [Fact]
    public async Task GetCurrentUserInfo_UserExists_ReturnsUserInfo()
    {
        // Arrange
        var faker = new Faker();
        var userId = faker.Random.Guid().ToString();
        var user = Fixture.Build<User>()
            .With(u => u.Id, userId)
            .With(u => u.Email, faker.Internet.Email())
            .Create();

        UserManagerMock.FindByIdAsync(userId).Returns(user);
        AccountsController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([
                    new Claim(ClaimTypes.NameIdentifier, userId)
                ]))
            }
        };

        // Act
        var result = await AccountsController.GetCurrentUserInfo();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var userInfo = okResult.Value;
        userInfo.Should().BeEquivalentTo(new
        {
            user.FirstName,
            user.LastName,
            user.Email,
            user.PhoneNumber,
            user.DateOfBirth
        });

        await UserManagerMock.Received(1).FindByIdAsync(userId);
    }
}