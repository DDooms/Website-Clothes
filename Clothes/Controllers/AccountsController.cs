using System.Net;
using System.Security.Claims;
using AutoMapper;
using Clothes.JwtFeatures;
using Clothes.Models.DTOs.AuthenticationDTOs;
using Clothes.Models.DTOs.PasswordDTOs;
using Clothes.Models.DTOs.RegistrationDTOs;
using Clothes.Models.DTOs.UpdateAccountDTOs;
using Clothes.Models.Entities;
using EmailService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Clothes.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly UserManager<User> UserManager;
    private readonly IMapper Mapper;
    private readonly JwtHandler JwtHandler;
    private readonly IEmailSender EmailSender;

    public AccountsController(UserManager<User> userManager, IMapper mapper, JwtHandler jwtHandler, IEmailSender emailSender)
    {
        UserManager = userManager;
        Mapper = mapper;
        JwtHandler = jwtHandler;
        EmailSender = emailSender;
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserForRegistrationDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = Mapper.Map<User>(model);
        var result = await UserManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new RegistrationResponseDto { IsSuccessfulRegistration = false, Errors = errors });
        }

        var token = await UserManager.GenerateEmailConfirmationTokenAsync(user);
    
        const string domain = "http://localhost:5054/api";
        var callbackUrl = $"{domain}/accounts/email-confirmation?userId={user.Id}&token={WebUtility.UrlEncode(token)}";

        var userN = user.FirstName + " " + user.LastName;
        var message = new Message(
            "bsyul942@gmail.com", 
            "B S", 
            user.Email, 
            userN, 
            "Email Confirmation Link", 
            $"Click the link to confirm your email: <a href=\"{callbackUrl}\">Confirm Email</a>"
        );

        await EmailSender.SendEmailAsync(message);

        await UserManager.AddToRoleAsync(user, "Visitor");

        return Created("", new RegistrationResponseDto { IsSuccessfulRegistration = true });
    }


    [HttpPost("authenticate")]
    public async Task<IActionResult> Authenticate([FromBody] UserForAuthenticationDto model)
    {
        var user = await UserManager.FindByNameAsync(model.Email);

        if (user is null)
        {
            return BadRequest("No such user");
        }

        if (!await UserManager.IsEmailConfirmedAsync(user))
        {
            return Unauthorized(new AuthResponseDto {ErrorMessage = "Email not confirmed"});
        }

        if (!await UserManager.CheckPasswordAsync(user, model.Password))
        {
            return Unauthorized(new AuthResponseDto {ErrorMessage = "Invalid authentication"});
        }

        var roles = await UserManager.GetRolesAsync(user);
        var (accessToken, refreshToken) = JwtHandler.CreateTokenWithRefreshToken(user, roles);
        
        // Hash the refresh token
        user.RefreshToken = JwtHandler.HashRefreshToken(refreshToken);
    
        // Set refresh token expiry only at login (fixed expiry time)
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        
        await UserManager.UpdateAsync(user);
        return Ok(new AuthResponseDto
        {
            IsAuthSuccessful = true, 
            Token = accessToken, 
            RefreshToken = refreshToken
        });
    }
    
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] AuthResponseDto? model)
    {
        if (model is null || string.IsNullOrWhiteSpace(model.RefreshToken))
            return BadRequest("Invalid request");

        var principal = JwtHandler.GetPrincipalFromExpiredToken(model.Token);
        var userId = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        var user = await UserManager.FindByIdAsync(userId);
        if (user == null || !JwtHandler.VerifyRefreshToken(user.RefreshToken, model.RefreshToken) || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            return Unauthorized("Invalid or expired refresh token");

        var roles = await UserManager.GetRolesAsync(user);
        var (newAccessToken, newRefreshToken) = JwtHandler.CreateTokenWithRefreshToken(user, roles);

        user.RefreshToken = JwtHandler.HashRefreshToken(newRefreshToken);

        await UserManager.UpdateAsync(user);

        return Ok(new
        {
            Token = newAccessToken,
            RefreshToken = newRefreshToken
        });
    }
    
    [HttpGet("email-confirmation")]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            return BadRequest("Invalid email confirmation request.");
        }

        var user = await UserManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        var result = await UserManager.ConfirmEmailAsync(user, token);
        if (result.Succeeded)
        {
            return Redirect("http://localhost:4200/signin");
        }
        else
        {
            return BadRequest("Email confirmation failed.");
        }
    }

    
    [HttpPost("resend-confirmation-email")]
    public async Task<IActionResult> ResendConfirmationEmailToken([FromBody] ResendConfirmationEmailDto model)
    {
        var user = await UserManager.FindByEmailAsync(model.Email);
        
        if (user == null || await UserManager.IsEmailConfirmedAsync(user))
        {
            return BadRequest("Invalid request.");
        }
        
        var token = await UserManager.GenerateEmailConfirmationTokenAsync(user);
    
        const string domain = "http://localhost:5054/api";
        var callbackUrl = $"{domain}/accounts/email-confirmation?userId={user.Id}&token={WebUtility.UrlEncode(token)}";

        var userN = user.FirstName + " " + user.LastName;
        var message = new Message(
            "bsyul942@gmail.com", 
            "B S", 
            user.Email, 
            userN, 
            "Email Confirmation Link", 
            $"Click the link to confirm your email: <a href=\"{callbackUrl}\">Confirm Email</a>"
        );

        await EmailSender.SendEmailAsync(message);
        
        return Ok(new RegistrationResponseDto { IsSuccessfulRegistration = true });
    }


    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }
        var user = await UserManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            return BadRequest("Invalid request");
        }
        
        var token = await UserManager.GeneratePasswordResetTokenAsync(user);
    
        const string domain = "http://localhost:5054/api";
        var callbackUrl = $"{domain}/accounts/reset-password?email={user.Email}&token={WebUtility.UrlEncode(token)}";

        var userN = user.FirstName + " " + user.LastName;
        var message = new Message(
            "bsyul942@gmail.com", 
            "B S", 
            user.Email, 
            userN, 
            "Email Confirmation Link", 
            $"Click the link to reset your password: <a href=\"{callbackUrl}\">Reset password</a>"
        );

        await EmailSender.SendEmailAsync(message);
        return Ok();
    }
    
    [HttpGet("reset-password")]
    public IActionResult ResetPasswordView([FromQuery] string email, [FromQuery] string token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
        {
            return BadRequest("Invalid reset password request.");
        }
        
        var resetUrl = $"http://localhost:4200/reset-password?email={email}&token={WebUtility.UrlEncode(token)}";
        return Redirect(resetUrl);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }
        var user = await UserManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            return BadRequest("Invalid request");
        }

        var result = await UserManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new {Errors = errors});
        }
        // Should always return a response, cos of cors policy
        return Ok();
    }
    
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUserInfo()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await UserManager.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found");

        var userInfo = new {
            user.FirstName,
            user.LastName,
            user.Email,
            user.PhoneNumber,
            user.DateOfBirth
        };
        return Ok(userInfo);
    }

    
    [Authorize] 
    [HttpPut("update")]
    public async Task<IActionResult> UpdateAccount([FromBody] UserForUpdateDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Get the currently logged-in user's ID (assuming JWT auth)
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await UserManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound("User not found");
        }

        user.FirstName = model.FirstName?.Trim();
        user.LastName = model.LastName?.Trim();
        user.PhoneNumber = string.IsNullOrEmpty(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
        user.DateOfBirth = model.DateOfBirth; 

        var result = await UserManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { Errors = errors });
        }

        return Ok(new UpdateResponseDto { IsSuccessfulUpdate = true });
    }
}