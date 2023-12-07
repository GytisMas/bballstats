
using BBallStats.Data;
using BBallStats.Data.Entities;
using BBallStats2;
using BBallStats2.Auth;
using BBallStats2.Auth.Model;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
        policy
        .WithOrigins("http://localhost:3000;https://whale-app-2-mg7ek.ondigitalocean.app/".Split(';'))
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        });
});

builder.Services.AddDbContext<ForumDbContext>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddTransient<JwtTokenService>();
builder.Services.AddScoped<AuthDbSeeder>();

builder.Services.AddIdentity<ForumRestUser, IdentityRole>()
    .AddEntityFrameworkStores<ForumDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;

}).AddJwtBearer(options =>
{
    options.TokenValidationParameters.ValidAudience = builder.Configuration["Jwt:ValidAudience"];
    options.TokenValidationParameters.ValidIssuer = builder.Configuration["Jwt:ValidIssuer"];
    options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]));
});

builder.Services.AddAuthorization();

var app = builder.Build();

var statisticsGroup = app.MapGroup("/api").WithValidationFilter();
Endpoints.GetStatisticEndpoints(statisticsGroup);

var usersGroup = app.MapGroup("/api").WithValidationFilter();
Endpoints.GetUserEndpoints(usersGroup);

var allRatings = app.MapGroup("api").WithValidationFilter();
Endpoints.GetAllRatingAlgorithms(allRatings);

var ratingsGroup = app.MapGroup("/api/users/{userId}").WithValidationFilter();
Endpoints.GetRatingAlgorithmEndpoints(ratingsGroup);

var algorithmStatisticsGroup = app.MapGroup("/api/users/{userId}/ratingAlgorithms/{ratingAlgorithmId}").WithValidationFilter();
Endpoints.GetAlgorithmStatisticEndpoints(algorithmStatisticsGroup);

var algorithmImpressionsGroup = app.MapGroup("/api/users/{userId}/ratingAlgorithms/{ratingAlgorithmId}").WithValidationFilter();
Endpoints.GetAlgorithmImpressionEndpoints(algorithmImpressionsGroup);

var teamsGroup = app.MapGroup("/api").WithValidationFilter();
Endpoints.GetTeamEndpoints(teamsGroup);

var playersGroup = app.MapGroup("/api/teams/{teamId}").WithValidationFilter();
Endpoints.GetPlayerEndpoints(playersGroup);

var playerStatisticsGroup = app.MapGroup("/api/teams/{teamId}/players/{playerId}").WithValidationFilter();
Endpoints.GetPlayerStatisticEndpoints(playerStatisticsGroup);

app.AddAuthApi();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

using var scope = app.Services.CreateScope();
var dbSeeder = scope.ServiceProvider.GetRequiredService<AuthDbSeeder>();

var dbContext = scope.ServiceProvider.GetRequiredService<ForumDbContext>();
dbContext.Database.Migrate();

await dbSeeder.SeedAsync();

app.Run();

// TODO: endpointuose validuot useri    // done kaip ir
// logout                               // done kaip ir
// prasitestuot viska (L2, L1)          //
#region Dto's, Validators

//public record CreateUserDto(string Username, string Password, string Email, UserType? Type);
//public record UpdateUserDto(string Password, string Email, UserType? Type);
//public record CreateRatingAlgorithmDto(string Formula, bool Promoted);
//public record UpdateRatingAlgorithmDto(string Formula, bool Promoted);
//public record CreateStatisticDto(string Name, string DisplayName, Visibility? Status);
//public record UpdateStatisticDto(string Name, string DisplayName, Visibility? Status);
//public record CreateTeamDto(string Name);
//public record UpdateTeamDto(string Name);
//public record CreateAlgorithmStatisticDto(int? StatisticId);
//public record UpdateAlgorithmStatisticDto(int? StatisticId);
//public record CreatePlayerStatisticDto(float Value, int? StatisticId);
//public record UpdatePlayerStatisticDto(float Value, int? StatisticId);
//public record CreateAlgorithmImpressionDto(bool Positive, int? UserId);
//public record UpdateAlgorithmImpressionDto(bool Positive, int? UserId);
//public record CreatePlayerDto(string Name, PlayerRole? Role);
//public record UpdatePlayerDto(string Name, PlayerRole? Role);

//public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
//{
//    public CreateUserDtoValidator()
//    {
//        RuleFor(dto => dto.Username).NotEmpty().NotNull().Length(min: 3, max: 20);
//        RuleFor(dto => dto.Password).NotEmpty().NotNull().Length(min: 3, max: 80);
//        RuleFor(dto => dto.Email).NotEmpty().NotNull().EmailAddress().Length(min: 3, max: 80);
//        RuleFor(dto => (int?)dto.Type).NotNull().NotEmpty().GreaterThanOrEqualTo(0).LessThanOrEqualTo(3);
//    }
//}

//public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
//{
//    public UpdateUserDtoValidator()
//    {
//        RuleFor(dto => dto.Password).NotEmpty().NotNull().Length(min: 3, max: 80);
//        RuleFor(dto => dto.Email).NotEmpty().NotNull().EmailAddress().Length(min: 3, max: 80);
//        RuleFor(dto => (int?)dto.Type).NotEmpty().NotNull().GreaterThanOrEqualTo(0).LessThanOrEqualTo(3);
//    }
//}

//public class CreateRatingAlgorithmDtoValidator : AbstractValidator<CreateRatingAlgorithmDto>
//{
//    public CreateRatingAlgorithmDtoValidator()
//    {
//        // TODO: make API method where algorithmStats are created based on formula
//        RuleFor(dto => dto.Formula).NotEmpty().NotNull()
//            .Matches(@"^\([0-9]+(\s[0-9]+)*\)\s[^\(\)]*(((?'Open'\()[^\(\)]*)+((?'Close-Open'\))[^\(\)]*)+)*(?(Open)(?!))$")
//            .Length(min: 3, max: 500);
//        RuleFor(dto => dto.Promoted).NotNull();
//    }
//}

//public class UpdateRatingAlgorithmDtoValidator : AbstractValidator<UpdateRatingAlgorithmDto>
//{
//    public UpdateRatingAlgorithmDtoValidator()
//    {
//        RuleFor(dto => dto.Formula).NotEmpty().NotNull()
//            .Matches(@"^\([0-9]+(\s[0-9]+)*\)\s[^\(\)]*(((?'Open'\()[^\(\)]*)+((?'Close-Open'\))[^\(\)]*)+)*(?(Open)(?!))$")
//            .Length(min: 3, max: 500);
//        RuleFor(dto => dto.Promoted).NotNull();
//    }
//}

//public class CreateStatisticDtoValidator : AbstractValidator<CreateStatisticDto>
//{
//    public CreateStatisticDtoValidator()
//    {
//        RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 3, max: 30);
//        RuleFor(dto => dto.DisplayName).NotEmpty().NotNull().Length(min: 3, max: 80);
//        RuleFor(dto => (int?)dto.Status).NotEmpty().NotNull().GreaterThanOrEqualTo(0).LessThanOrEqualTo(1);
//    }
//}

//public class UpdateStatisticDtoValidator : AbstractValidator<UpdateStatisticDto>
//{
//    public UpdateStatisticDtoValidator()
//    {
//        RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 3, max: 30);
//        RuleFor(dto => dto.DisplayName).NotEmpty().NotNull().Length(min: 3, max: 80);
//        RuleFor(dto => (int?)dto.Status).NotEmpty().NotNull().GreaterThanOrEqualTo(0).LessThanOrEqualTo(1);
//    }
//}

//public class CreateTeamDtoValidator : AbstractValidator<CreateTeamDto>
//{
//    public CreateTeamDtoValidator()
//    {
//        RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 3, max: 40);
//    }
//}

//public class UpdateTeamDtoValidator : AbstractValidator<UpdateTeamDto>
//{
//    public UpdateTeamDtoValidator()
//    {
//        RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 3, max: 40);
//    }
//}

//public class CreateAlgorithmStatisticDtoValidator : AbstractValidator<CreateAlgorithmStatisticDto>
//{
//    public CreateAlgorithmStatisticDtoValidator()
//    {
//        RuleFor(dto => dto.StatisticId).NotEmpty().NotNull().GreaterThan(-1);
//    }
//}

//public class UpdateAlgorithmStatisticDtoValidator : AbstractValidator<UpdateAlgorithmStatisticDto>
//{
//    public UpdateAlgorithmStatisticDtoValidator()
//    {
//        RuleFor(dto => dto.StatisticId).NotEmpty().NotNull().GreaterThan(-1);
//    }
//}

//public class CreatePlayerStatisticDtoValidator : AbstractValidator<CreatePlayerStatisticDto>
//{
//    public CreatePlayerStatisticDtoValidator()
//    {
//        RuleFor(dto => dto.StatisticId).NotEmpty().NotNull().GreaterThan(-1);
//        RuleFor(dto => dto.Value).NotEmpty().NotNull();
//    }
//}

//public class UpdatePlayerStatisticDtoValidator : AbstractValidator<UpdatePlayerStatisticDto>
//{
//    public UpdatePlayerStatisticDtoValidator()
//    {
//        RuleFor(dto => dto.StatisticId).NotEmpty().NotNull().GreaterThan(-1);
//        RuleFor(dto => dto.Value).NotEmpty().NotNull();
//    }
//}

//public class CreateAlgorithmImpressionDtoValidator : AbstractValidator<CreateAlgorithmImpressionDto>
//{
//    public CreateAlgorithmImpressionDtoValidator()
//    {
//        RuleFor(dto => dto.UserId).NotEmpty().NotNull().GreaterThan(-1);
//        RuleFor(dto => dto.Positive).NotNull();
//    }
//}

//public class UpdateAlgorithmImpressionDtoValidator : AbstractValidator<UpdateAlgorithmImpressionDto>
//{
//    public UpdateAlgorithmImpressionDtoValidator()
//    {
//        RuleFor(dto => dto.UserId).NotEmpty().NotNull().GreaterThan(-1);
//        RuleFor(dto => dto.Positive).NotNull();
//    }
//}

//public class CreatePlayerDtoValidator : AbstractValidator<CreatePlayerDto>
//{
//    public CreatePlayerDtoValidator()
//    {
//        RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 3, max: 40);
//        RuleFor(dto => (int?)dto.Role).NotEmpty().NotNull().GreaterThanOrEqualTo(0).LessThanOrEqualTo(4);
//    }
//}

//public class UpdatePlayerDtoValidator : AbstractValidator<UpdatePlayerDto>
//{
//    public UpdatePlayerDtoValidator()
//    {
//        RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 3, max: 40);
//        RuleFor(dto => (int?)dto.Role).NotEmpty().NotNull().GreaterThanOrEqualTo(0).LessThanOrEqualTo(4);
//    }
//}
#endregion