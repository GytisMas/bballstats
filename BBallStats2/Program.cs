
using BBallStats.Data;
using BBallStats.Data.Entities;
using BBallStats2;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ForumDbContext>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
var app = builder.Build();

var usersGroup = app.MapGroup("/api").WithValidationFilter();

Endpoints.GetUserEndpoints(usersGroup);

var ratingsGroup = app.MapGroup("/api/users/{userId}").WithValidationFilter();
Endpoints.GetRatingAlgorithmEndpoints(ratingsGroup);

var statisticsGroup = app.MapGroup("/api").WithValidationFilter();
Endpoints.GetStatisticEndpoints(statisticsGroup);

var teamsGroup = app.MapGroup("/api").WithValidationFilter();
Endpoints.GetTeamEndpoints(teamsGroup);

var algorithmStatisticsGroup = app.MapGroup("/api/users/{userId}/ratingAlgorithms/{ratingAlgorithmId}").WithValidationFilter();
Endpoints.GetAlgorithmStatisticEndpoints(algorithmStatisticsGroup);

app.Run();

#region Dto's, Validators

public record CreateUserDto(string Username, string Password, string Email, UserType Type);
public record UpdateUserDto(string Password, string Email, UserType Type);
public record CreateRatingAlgorithmDto(string Formula);
public record UpdateRatingAlgorithmDto(string Formula);
public record CreateStatisticDto(string Name, string DisplayName, Visibility Status);
public record UpdateStatisticDto(string Name, string DisplayName, Visibility Status);
public record CreateTeamDto(string Name);
public record UpdateTeamDto(string Name);
public record CreateAlgorithmStatisticDto(int statisticId);
public record UpdateAlgorithmStatisticDto(int statisticId);

public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleFor(dto => dto.Username).NotEmpty().NotNull().Length(min: 3, max: 20);
        RuleFor(dto => dto.Password).NotEmpty().NotNull().Length(min: 3, max: 80);
        RuleFor(dto => dto.Email).NotEmpty().NotNull().EmailAddress().Length(min: 3, max: 80);
        RuleFor(dto => ((int?)dto.Type)).NotNull().NotEmpty().GreaterThanOrEqualTo(0).LessThanOrEqualTo(1);
    }
}

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        RuleFor(dto => dto.Password).NotEmpty().NotNull().Length(min: 3, max: 80);
        RuleFor(dto => dto.Email).NotEmpty().NotNull().EmailAddress().Length(min: 3, max: 80);
        RuleFor(dto => Enum.IsDefined(typeof(UserType), dto.Type) ? ((int?)dto.Type) : null).NotEmpty().NotNull().GreaterThanOrEqualTo(0).LessThanOrEqualTo(1);
    }
}

public class CreateRatingAlgorithmDtoValidator : AbstractValidator<CreateRatingAlgorithmDto>
{
    public CreateRatingAlgorithmDtoValidator()
    {
        RuleFor(dto => dto.Formula).NotEmpty().NotNull().Length(min: 3, max: 20);
    }
}

public class UpdateRatingAlgorithmDtoValidator : AbstractValidator<UpdateRatingAlgorithmDto>
{
    public UpdateRatingAlgorithmDtoValidator()
    {
        RuleFor(dto => dto.Formula).NotEmpty().NotNull().Length(min: 3, max: 20);
    }
}

public class CreateStatisticDtoValidator : AbstractValidator<CreateStatisticDto>
{
    public CreateStatisticDtoValidator()
    {
        RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 3, max: 20);
        RuleFor(dto => dto.DisplayName).NotEmpty().NotNull().Length(min: 3, max: 80);
        RuleFor(dto => ((int?)dto.Status)).NotEmpty().NotNull().GreaterThanOrEqualTo(0).LessThanOrEqualTo(1);
    }
}

public class UpdateStatisticDtoValidator : AbstractValidator<UpdateStatisticDto>
{
    public UpdateStatisticDtoValidator()
    {
        RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 3, max: 20);
        RuleFor(dto => dto.DisplayName).NotEmpty().NotNull().Length(min: 3, max: 80);
        RuleFor(dto => ((int?)dto.Status)).NotEmpty().NotNull().GreaterThanOrEqualTo(0).LessThanOrEqualTo(1);
    }
}

public class CreateTeamDtoValidator : AbstractValidator<CreateTeamDto>
{
    public CreateTeamDtoValidator()
    {
        RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 3, max: 30);
    }
}

public class UpdateTeamDtoValidator : AbstractValidator<UpdateTeamDto>
{
    public UpdateTeamDtoValidator()
    {
        RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 3, max: 30);
    }
}

public class CreateAlgorithmStatisticDtoValidator : AbstractValidator<CreateAlgorithmStatisticDto>
{
    public CreateAlgorithmStatisticDtoValidator()
    {
        RuleFor(dto => dto.statisticId).NotEmpty().NotNull().GreaterThan(-1);
    }
}

public class UpdateAlgorithmStatisticDtoValidator : AbstractValidator<UpdateAlgorithmStatisticDto>
{
    public UpdateAlgorithmStatisticDtoValidator()
    {
        RuleFor(dto => dto.statisticId).NotEmpty().NotNull().GreaterThan(-1);
    }
}
#endregion