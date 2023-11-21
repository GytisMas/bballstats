using BBallStats2.Auth.Model;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using O9d.AspNet.FluentValidation;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BBallStats2.Auth
{
    public static class AuthEndpoints
    {
        public static void AddAuthApi(this WebApplication app)
        {
            app.MapPost("api/register", async (UserManager<ForumRestUser> userManager, [Validate] CreateUserDto createUserDto) =>
            {
                var user = await userManager.FindByNameAsync(createUserDto.Username);
                if (user != null)
                    return Results.UnprocessableEntity("User name already taken");

                var newUser = new ForumRestUser()
                {
                    UserName = createUserDto.Username,
                    Email = createUserDto.Email
                };

                var createUserResult = await userManager.CreateAsync(newUser, createUserDto.Password);
                if (!createUserResult.Succeeded)
                    return Results.UnprocessableEntity();

                await userManager.AddToRoleAsync(newUser, ForumRoles.Regular);

                return Results.Created($"/api/login", new UserDto(newUser.Id, newUser.UserName, newUser.Email));
            });

            app.MapPost("api/login", async (UserManager<ForumRestUser> userManager, JwtTokenService jwtTokenService, [Validate] LoginUserDto loginUserDto) =>
            {
                var user = await userManager.FindByNameAsync(loginUserDto.Username);
                if (user == null)
                    return Results.UnprocessableEntity("Username or password was incorrect");

                var passwordIsValid = await userManager.CheckPasswordAsync(user, loginUserDto.Password);
                if (!passwordIsValid)
                    return Results.UnprocessableEntity("Username or password was incorrect");

                user.ForceRelogin = false;
                await userManager.UpdateAsync(user);

                var roles = await userManager.GetRolesAsync(user);
                var accessToken = jwtTokenService.CreateAccessToken(user.UserName, user.Id, roles);
                var refreshToken = jwtTokenService.CreateRefreshToken(user.Id);

                return Results.Ok(new SuccessfulLoginDto(accessToken, refreshToken));
            });

            app.MapPost("api/logout", async (UserManager<ForumRestUser> userManager, JwtTokenService jwtTokenService, HttpContext httpContext) =>
            {
                var user = await userManager.FindByIdAsync(httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub));
                if (user == null)
                    return Results.UnprocessableEntity($"User not found");
                user.ForceRelogin = true;
                await userManager.UpdateAsync(user);
                return Results.Ok();
            });

            app.MapPost("api/changePassword", async (HttpContext httpContext, JwtTokenService jwtTokenService, UserManager<ForumRestUser> userManager, [Validate] ChangePasswordDto changePasswordDto) =>
            {
                var user = await userManager.FindByIdAsync(httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub));
                if (user == null)
                    return Results.NotFound($"User ({httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)}) not found");
                var changePasswordResult = await userManager.ChangePasswordAsync(user, changePasswordDto.OldPassword, changePasswordDto.NewPassword);
                if (!changePasswordResult.Succeeded)
                    return Results.UnprocessableEntity();

                return Results.Ok(new UserDto(user.Id, user.UserName, user.Email));
            });

            app.MapPost("api/accessToken", async (UserManager<ForumRestUser> userManager, JwtTokenService jwtTokenService, RefreshAccessTokenDto refreshAccessTokenDto) =>
            {
                if (!jwtTokenService.TryParseRefreshToken(refreshAccessTokenDto.RefreshToken, out var claims))
                {
                    return Results.UnprocessableEntity("Couldn't parse refresh token");
                }

                var userId = claims.FindFirstValue(JwtRegisteredClaimNames.Sub);

                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                    return Results.UnprocessableEntity("Invalid token");

                if (user.ForceRelogin)
                    return Results.UnprocessableEntity("User has to log in");

                var roles = await userManager.GetRolesAsync(user);
                var accessToken = jwtTokenService.CreateAccessToken(user.UserName, user.Id, roles);
                var refreshToken = jwtTokenService.CreateRefreshToken(user.Id);

                return Results.Ok(new SuccessfulLoginDto(accessToken, refreshToken));
            });
        }
    }
}

public record RegisterUserDto(string Username, string Password, string Email);
public record LoginUserDto(string Username, string Password);
public record ChangePasswordDto(string OldPassword, string NewPassword);
public record SuccessfulLoginDto(string AccessToken, string RefreshToken);
public record RefreshAccessTokenDto(string RefreshToken);

public class RegisterUserDtoValidator : AbstractValidator<RegisterUserDto>
{
    public RegisterUserDtoValidator()
    {
        RuleFor(dto => dto.Username).NotEmpty().NotNull().Length(min: 3, max: 20);
        RuleFor(dto => dto.Password).NotEmpty().NotNull().Length(min: 3, max: 80);
        RuleFor(dto => dto.Email).NotEmpty().NotNull().EmailAddress().Length(min: 3, max: 80);
    }
}
public class LoginUserDtoValidator : AbstractValidator<LoginUserDto>
{
    public LoginUserDtoValidator()
    {
        RuleFor(dto => dto.Username).NotEmpty().NotNull().Length(min: 3, max: 20);
        RuleFor(dto => dto.Password).NotEmpty().NotNull().Length(min: 3, max: 80);
    }
}

public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordDtoValidator()
    {
        RuleFor(dto => dto.OldPassword).NotEmpty().NotNull().Length(min: 3, max: 80);
        RuleFor(dto => dto.NewPassword).NotEmpty().NotNull().Length(min: 3, max: 80);
    }
}