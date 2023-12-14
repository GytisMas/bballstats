using BBallStats.Data.Entities;
using BBallStats.Data;
using O9d.AspNet.FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using static BBallStats.Data.Entities.RatingAlgorithm;
using static BBallStats.Data.Entities.AlgorithmStatistic;
using static BBallStats.Data.Entities.AlgorithmImpression;
using static BBallStats.Data.Entities.Team;
using static BBallStats.Data.Entities.Player;
using static BBallStats.Data.Entities.PlayerStatistic;
using static BBallStats.Data.Entities.Statistic;
using System.Threading;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using BBallStats2.Auth.Model;
using FluentValidation;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Net.Http;

namespace BBallStats2
{
    public static class Endpoints
    {
        public static void GetUserEndpoints(RouteGroupBuilder usersGroup)
        {
            usersGroup.MapGet("users", [Authorize(Roles = ForumRoles.Admin)] async (UserManager<ForumRestUser> userManager, HttpContext httpContext, ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                if (!httpContext.User.IsInRole(ForumRoles.Admin))
                {
                    return Results.Forbid();
                }
                var userList = (await dbContext.Users.ToListAsync(cancellationToken))
                .Select(o => new UserWithoutRolesDto(o.Id, o.UserName, o.Email));

                return Results.Ok(userList);
            });

            usersGroup.MapGet("users/{userId}", async (string userId, UserManager<ForumRestUser> userManager, ForumDbContext dbContext) =>
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                    return Results.NotFound();
                var roles = await userManager.GetRolesAsync(user);
                return Results.Ok(new UserDto(user.Id, user.UserName, user.Email, roles));
            });

            usersGroup.MapPost("users", [Authorize(Roles = ForumRoles.Admin)] async (UserManager<ForumRestUser> userManager, HttpContext httpContext, [Validate] CreateUserDto createUserDto) =>
            {
                var user = await userManager.FindByNameAsync(createUserDto.Username);
                if (user != null)
                    return Results.UnprocessableEntity("User name already taken");

                if (!httpContext.User.IsInRole(ForumRoles.Admin))
                {
                    return Results.Forbid();
                }

                var newUser = new ForumRestUser()
                {
                    UserName = createUserDto.Username,
                    Email = createUserDto.Email
                };

                var createUserResult = await userManager.CreateAsync(newUser, createUserDto.Password);
                if (!createUserResult.Succeeded)
                {
                    string errors = "";
                    foreach (var item in createUserResult.Errors)
                    {
                        errors += item.Description + ",, ";
                    }
                    return Results.UnprocessableEntity("User not created" + " | " + errors + " |");
                }

                List<string> roles = new List<string>();

                if ((createUserDto.Role & 1) != 0)
                    roles.Add(ForumRoles.Admin);
                if ((createUserDto.Role & 2) != 0)
                    roles.Add(ForumRoles.Moderator);
                if ((createUserDto.Role & 4) != 0)
                    roles.Add(ForumRoles.Curator);
                if ((createUserDto.Role & 8) != 0)
                    roles.Add(ForumRoles.Regular);
                await userManager.AddToRolesAsync(newUser, roles);

                return Results.Created($"/api/Users/{newUser.Id}", new UserDto(newUser.Id, newUser.UserName, newUser.Email, roles));
            });

            usersGroup.MapPut("users/{userId}", [Authorize(Roles = ForumRoles.Admin)] async (string userId, HttpContext httpContext, UserManager<ForumRestUser> userManager, [Validate] UpdateUserDto updateUserDto) =>
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                    return Results.NotFound();

                if (!httpContext.User.IsInRole(ForumRoles.Admin))
                {
                    return Results.Forbid();
                }

                var changePasswordResult = await userManager.ChangePasswordAsync(user, updateUserDto.OldPassword, updateUserDto.NewPassword);
                if (!changePasswordResult.Succeeded)
                    return Results.UnprocessableEntity("Password not changed successfully");

                user.Email = updateUserDto.Email;
                await userManager.UpdateAsync(user);
                List<string> roles = new List<string>();

                if ((updateUserDto.Role & 1) != 0)
                    roles.Add(ForumRoles.Admin);
                if ((updateUserDto.Role & 2) != 0)
                    roles.Add(ForumRoles.Moderator);
                if ((updateUserDto.Role & 4) != 0)
                    roles.Add(ForumRoles.Curator);
                if ((updateUserDto.Role & 8) != 0)
                    roles.Add(ForumRoles.Regular);

                await userManager.RemoveFromRolesAsync(user, ForumRoles.All);
                await userManager.AddToRolesAsync(user, roles);

                return Results.Ok(new UserWithoutRolesDto(user.Id, user.UserName, user.Email));
            });

            usersGroup.MapDelete("users/{userId}", [Authorize(Roles = ForumRoles.Admin)] async (string userId, HttpContext httpContext, UserManager<ForumRestUser> userManager) =>
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                    return Results.NotFound();
                if (!httpContext.User.IsInRole(ForumRoles.Admin))
                {
                    return Results.Forbid();
                }

                var deleteUserResult = await userManager.DeleteAsync(user);

                return Results.NoContent();
            });
        }

        public static void GetAllRatingAlgorithms(RouteGroupBuilder ratingsGroup)
        {
            ratingsGroup.MapGet("ratingAlgorithms", async (ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {

                var ratingAlgorithms = (await dbContext.RatingAlgorithms.ToListAsync(cancellationToken))
                    .Select(o => new RatingAlgorithmDto(o.Id, o.Formula, o.Promoted, o.UserId));
                return Results.Ok(ratingAlgorithms);
            });
        }

        public static void GetRatingAlgorithmEndpoints(RouteGroupBuilder ratingsGroup)
        {
            ratingsGroup.MapGet("ratingAlgorithms", async (string userId, UserManager<ForumRestUser> userManager, ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithms = (await dbContext.RatingAlgorithms.ToListAsync(cancellationToken))
                    .Where(o => o.UserId == userId)
                    .Select(o => new RatingAlgorithmDto(o.Id, o.Formula, o.Promoted, userId));

                return Results.Ok(ratingAlgorithms);
            });

            ratingsGroup.MapGet("ratingAlgorithms/{ratingAlgorithmId}", async (string userId, UserManager<ForumRestUser> userManager, int ratingAlgorithmId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.User.Id == userId);
                if (ratingAlgorithm == null)
                    return Results.NotFound();

                return Results.Ok(new RatingAlgorithmDto(ratingAlgorithm.Id, ratingAlgorithm.Formula, ratingAlgorithm.Promoted, ratingAlgorithm.UserId));
            });

            ratingsGroup.MapPost("ratingAlgorithms", [Authorize(Roles = ForumRoles.Regular)] async (string userId, UserManager<ForumRestUser> userManager, [Validate] CreateRatingAlgorithmDto createRatingAlgorithmDto, HttpContext httpContext, ForumDbContext dbContext) =>
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                    return Results.NotFound("user not found");
                if (!httpContext.User.IsInRole(ForumRoles.Regular)
                    || httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != userId)
                {
                    return Results.UnprocessableEntity($"{httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)} | {userId}");
                }

                List<int> formulaStatIds = createRatingAlgorithmDto.Formula.Split(") ")[0]
                    .Remove(0, 1)
                    .Split(" ", StringSplitOptions.RemoveEmptyEntries)
                    .Select(Int32.Parse).ToList<int>();

                var Stats = (await dbContext.Statistics.ToListAsync())
                    .Where(s => formulaStatIds.Contains(s.Id)).ToList();
                if (Stats.Count < formulaStatIds.Count)
                    return Results.NotFound("statistic not found");

                var ratingAlgorithm = new RatingAlgorithm()
                {
                    Formula = createRatingAlgorithmDto.Formula,
                    Promoted = createRatingAlgorithmDto.Promoted,
                    UserId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                };

                dbContext.RatingAlgorithms.Add(ratingAlgorithm);

                foreach (var statistic in Stats)
                {
                    var algorithmStatistic = new AlgorithmStatistic()
                    {
                        Algorithm = ratingAlgorithm,
                        StatType = statistic
                    };
                    dbContext.AlgorithmStatistics.Add(algorithmStatistic);
                }

                await dbContext.SaveChangesAsync();

                return Results.Created($"/api/Users/{user.Id}/RatingAlgorithms/{ratingAlgorithm.Id}", new RatingAlgorithmDto(ratingAlgorithm.Id, ratingAlgorithm.Formula, ratingAlgorithm.Promoted, ratingAlgorithm.User.Id));
            });
            ratingsGroup.MapPut("ratingAlgorithms/{ratingAlgorithmId}", [Authorize(Roles = ForumRoles.Regular)] async (string userId, UserManager<ForumRestUser> userManager, HttpContext httpContext, int ratingAlgorithmId, [Validate] UpdateRatingAlgorithmDto updateRatingAlgorithmDto, ForumDbContext dbContext) =>
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.User.Id == userId);
                if (ratingAlgorithm == null)
                    return Results.NotFound();

                if (!httpContext.User.IsInRole(ForumRoles.Regular)
                    || httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != userId)
                    return Results.Forbid();


                List<int> newFormulaStatIds = updateRatingAlgorithmDto.Formula.Split(") ")[0]
                    .Remove(0, 1)
                    .Split(" ", StringSplitOptions.RemoveEmptyEntries)
                    .Select(Int32.Parse).ToList<int>();
                var NewStats = (await dbContext.Statistics.ToListAsync())
                    .Where(s => newFormulaStatIds.Contains(s.Id)).ToList();
                if (NewStats.Count < newFormulaStatIds.Count)
                    return Results.NotFound();

                List<int> oldFormulaStatIds = ratingAlgorithm.Formula.Split(") ")[0]
                    .Remove(0, 1)
                    .Split(" ", StringSplitOptions.RemoveEmptyEntries)
                    .Select(Int32.Parse).ToList<int>();
                await dbContext.RatingAlgorithms.ToListAsync();
                var OldAlgoStats = (await dbContext.AlgorithmStatistics.ToListAsync())
                    .Where(s => s.Algorithm.Id == ratingAlgorithmId).ToList();
                
                foreach (var oldStat in OldAlgoStats)
                    dbContext.Remove(oldStat);

                foreach (var statistic in NewStats)
                {
                    var algorithmStatistic = new AlgorithmStatistic()
                    {
                        Algorithm = ratingAlgorithm,
                        StatType = statistic
                    };
                    dbContext.AlgorithmStatistics.Add(algorithmStatistic);
                }

                ratingAlgorithm.Formula = updateRatingAlgorithmDto.Formula;
                ratingAlgorithm.Promoted = updateRatingAlgorithmDto.Promoted;

                dbContext.Update(ratingAlgorithm);
                await dbContext.SaveChangesAsync();

                return Results.Ok(new RatingAlgorithmDto(ratingAlgorithm.Id, ratingAlgorithm.Formula, ratingAlgorithm.Promoted, ratingAlgorithm.UserId));
            });

            ratingsGroup.MapDelete("ratingAlgorithms/{ratingAlgorithmId}", [Authorize(Roles = ForumRoles.Regular)] async (string userId, UserManager<ForumRestUser> userManager, int ratingAlgorithmId, ForumDbContext dbContext, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.User.Id == userId);
                if (ratingAlgorithm == null)
                    return Results.NotFound();

                if (!httpContext.User.IsInRole(ForumRoles.Regular)
                    || httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != userId)
                    return Results.Forbid();

                dbContext.Remove(ratingAlgorithm);
                await dbContext.SaveChangesAsync();

                return Results.NoContent();
            });
        }

        public static void GetStatisticEndpoints(RouteGroupBuilder statisticsGroup)
        {
            statisticsGroup.MapGet("statistics", async (ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                return (await dbContext.Statistics.ToListAsync(cancellationToken))
                    .Select(o => new StatisticDto(o.Id, o.Name, o.DisplayName, (int)o.Status));
            });

            statisticsGroup.MapGet("statistics/v", async (ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                return (await dbContext.Statistics.ToListAsync(cancellationToken))
                    .Where(o => o.Status == Visibility.Public)
                    .Select(o => new StatisticDto(o.Id, o.Name, o.DisplayName, (int)o.Status));
            });

            statisticsGroup.MapGet("statistics/{statisticId}", async (int statisticId, ForumDbContext dbContext) =>
            {
                var statistic = await dbContext.Statistics.FirstOrDefaultAsync(u => u.Id == statisticId);
                if (statistic == null)
                    return Results.NotFound();

                return Results.Ok(new StatisticDto(statistic.Id, statistic.Name, statistic.DisplayName, (int)statistic.Status));
            });

            statisticsGroup.MapPost("statistics", [Authorize(Roles = ForumRoles.Curator)] async ([Validate] CreateStatisticDto createStatisticDto, HttpContext httpContext, ForumDbContext dbContext) =>
            {
                if (!httpContext.User.IsInRole(ForumRoles.Curator))
                    return Results.Forbid();

                var statistic = new Statistic()
                {
                    Name = createStatisticDto.Name,
                    DisplayName = createStatisticDto.DisplayName,
                    Status = (Visibility)createStatisticDto.Status
                };

                dbContext.Statistics.Add(statistic);
                await dbContext.SaveChangesAsync();

                return Results.Created($"/api/statistics/{statistic.Id}", new StatisticDto(statistic.Id, statistic.Name, statistic.DisplayName, (int)statistic.Status));
            });

            statisticsGroup.MapPut("statistics/{statisticId}", [Authorize(Roles = ForumRoles.Curator)] async (int statisticId, HttpContext httpContext, [Validate] UpdateStatisticDto updateStatisticDto, ForumDbContext dbContext) =>
            {
                var statistic = await dbContext.Statistics.FirstOrDefaultAsync(u => u.Id == statisticId);
                if (statistic == null)
                    return Results.NotFound();

                if (!httpContext.User.IsInRole(ForumRoles.Curator))
                    return Results.Forbid();

                statistic.Name = updateStatisticDto.Name;
                statistic.DisplayName = updateStatisticDto.DisplayName;
                statistic.Status = (Visibility)updateStatisticDto.Status;

                dbContext.Update(statistic);
                await dbContext.SaveChangesAsync();

                return Results.Ok(new StatisticDto(statistic.Id, statistic.Name, statistic.DisplayName, (int)statistic.Status));
            });

            statisticsGroup.MapDelete("statistics/{statisticId}", [Authorize(Roles = ForumRoles.Curator)] async (int statisticId, HttpContext httpContext, ForumDbContext dbContext) =>
            {
                var statistic = await dbContext.Statistics.FirstOrDefaultAsync(u => u.Id == statisticId);
                if (statistic == null)
                    return Results.NotFound();

                if (!httpContext.User.IsInRole(ForumRoles.Curator))
                    return Results.Forbid();

                dbContext.Remove(statistic);
                await dbContext.SaveChangesAsync();

                return Results.NoContent();
            });
        }

        public static void GetTeamEndpoints(RouteGroupBuilder teamsGroup)
        {
            teamsGroup.MapGet("teams", async (ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                return (await dbContext.Teams.ToListAsync(cancellationToken));
            });

            teamsGroup.MapGet("teams/{teamId}", async (int teamId, ForumDbContext dbContext) =>
            {
                var team = await dbContext.Teams.FirstOrDefaultAsync(u => u.Id == teamId);
                if (team == null)
                    return Results.NotFound();

                return Results.Ok(team);
            });

            teamsGroup.MapPost("teams", [Authorize(Roles = ForumRoles.Moderator)] async ([Validate] CreateTeamDto createTeamDto, HttpContext httpContext, ForumDbContext dbContext) =>
            {
                if (!httpContext.User.IsInRole(ForumRoles.Moderator))
                    return Results.Forbid();

                var team = new Team()
                {
                    Name = createTeamDto.Name
                };

                dbContext.Teams.Add(team);
                await dbContext.SaveChangesAsync();

                return Results.Created($"/api/Teams/{team.Id}", team);
            });

            teamsGroup.MapPut("teams/{teamId}", [Authorize(Roles = ForumRoles.Moderator)] async (int teamId, [Validate] UpdateTeamDto updateTeamDto, HttpContext httpContext, ForumDbContext dbContext) =>
            {
                var team = await dbContext.Teams.FirstOrDefaultAsync(u => u.Id == teamId);
                if (team == null)
                    return Results.NotFound();

                if (!httpContext.User.IsInRole(ForumRoles.Moderator))
                    return Results.Forbid();

                team.Name = updateTeamDto.Name;

                dbContext.Update(team);
                await dbContext.SaveChangesAsync();

                return Results.Ok(team);
            });

            teamsGroup.MapDelete("teams/{teamId}", [Authorize(Roles = ForumRoles.Moderator)] async (int teamId, HttpContext httpContext, ForumDbContext dbContext) =>
            {
                var team = await dbContext.Teams.FirstOrDefaultAsync(u => u.Id == teamId);
                if (team == null)
                    return Results.NotFound();

                if (!httpContext.User.IsInRole(ForumRoles.Moderator))
                    return Results.Forbid();

                dbContext.Remove(team);
                await dbContext.SaveChangesAsync();

                return Results.NoContent();
            });
        }

        public static void GetAlgorithmStatisticEndpoints(RouteGroupBuilder algoStatsGroup)
        {
            algoStatsGroup.MapGet("algorithmStatistics", async (string userId, UserManager<ForumRestUser> userManager, int ratingAlgorithmId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.User.Id == userId);
                if (ratingAlgorithm == null)
                    return Results.NotFound();

                await dbContext.Statistics.ToListAsync(cancellationToken);

                var algorithmStatistics = (await dbContext.AlgorithmStatistics.ToListAsync(cancellationToken))
                    .Where(o => o.Algorithm == ratingAlgorithm)
                    .Select(o => new AlgorithmStatisticDto(
                        o.Id,
                        o.StatType.Id, 
                        ratingAlgorithmId));

                return Results.Ok(algorithmStatistics);
            });

            algoStatsGroup.MapGet("algorithmStatistics/{algorithmStatisticId}", async (string userId, UserManager<ForumRestUser> userManager, int ratingAlgorithmId, int algorithmStatisticId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.User.Id == userId);
                if (ratingAlgorithm == null)
                    return Results.NotFound();

                var algorithmStatistic = await dbContext.AlgorithmStatistics.FirstOrDefaultAsync(r => r.Id == algorithmStatisticId && r.Algorithm.Id == ratingAlgorithmId);
                if (algorithmStatistic == null)
                    return Results.NotFound();

                await dbContext.Statistics.ToListAsync(cancellationToken);

                return Results.Ok(new AlgorithmStatisticDto(algorithmStatistic.Id, algorithmStatistic.StatType.Id, ratingAlgorithmId));
            });

            algoStatsGroup.MapPost("algorithmStatistics", [Authorize(Roles = ForumRoles.Regular)] async (string userId, HttpContext httpContext, UserManager<ForumRestUser> userManager, int ratingAlgorithmId, [Validate] CreateAlgorithmStatisticDto createAlgorithmStatisticDto, ForumDbContext dbContext) =>
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.User.Id == userId);
                if (ratingAlgorithm == null)
                    return Results.NotFound();

                if (!httpContext.User.IsInRole(ForumRoles.Regular)
                    || httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != userId)
                    return Results.Forbid();

                var statistic = await dbContext.Statistics.FirstOrDefaultAsync(r => r.Id == createAlgorithmStatisticDto.StatisticId);
                if (statistic == null)
                    return Results.NotFound();

                var algorithmStatistic = new AlgorithmStatistic()
                {
                    Algorithm = ratingAlgorithm,
                    StatType = statistic
                };

                dbContext.AlgorithmStatistics.Add(algorithmStatistic);
                await dbContext.SaveChangesAsync();

                return Results.Created($"/api/Users/{user.Id}/RatingAlgorithms/{ratingAlgorithm.Id}/AlgorithmStatistics/{algorithmStatistic.Id}",
                    new AlgorithmStatisticDto(algorithmStatistic.Id, algorithmStatistic.StatType.Id, ratingAlgorithmId));
            });

            algoStatsGroup.MapPut("algorithmStatistics/{algorithmStatisticId}", [Authorize(Roles = ForumRoles.Regular)] async (string userId, HttpContext httpContext, UserManager<ForumRestUser> userManager, int ratingAlgorithmId, int algorithmStatisticId, [Validate] UpdateAlgorithmStatisticDto updateAlgorithmStatisticDto, ForumDbContext dbContext) =>
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.User.Id == userId);
                if (ratingAlgorithm == null)
                    return Results.NotFound();

                var algorithmStatistic = await dbContext.AlgorithmStatistics.FirstOrDefaultAsync(r => r.Id == algorithmStatisticId && r.Algorithm.Id == ratingAlgorithmId);
                if (algorithmStatistic == null)
                    return Results.NotFound();

                if (!httpContext.User.IsInRole(ForumRoles.Regular)
                    || httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != userId)
                    return Results.Forbid();

                var statistic = await dbContext.Statistics.FirstOrDefaultAsync(r => r.Id == updateAlgorithmStatisticDto.StatisticId);
                if (statistic == null)
                    return Results.NotFound();

                algorithmStatistic.StatType = statistic;

                dbContext.Update(algorithmStatistic);
                await dbContext.SaveChangesAsync();

                return Results.Ok(new AlgorithmStatisticDto(algorithmStatistic.Id, algorithmStatistic.StatType.Id, ratingAlgorithmId));
            });

            algoStatsGroup.MapDelete("algorithmStatistics/{algorithmStatisticId}", [Authorize(Roles = ForumRoles.Regular)] async (string userId, HttpContext httpContext, UserManager<ForumRestUser> userManager, int ratingAlgorithmId, int algorithmStatisticId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.User.Id == userId);
                if (ratingAlgorithm == null)
                    return Results.NotFound();

                var algorithmStatistic = await dbContext.AlgorithmStatistics.FirstOrDefaultAsync(r => r.Id == algorithmStatisticId && r.Algorithm.Id == ratingAlgorithmId);
                if (algorithmStatistic == null)
                    return Results.NotFound();

                if (!httpContext.User.IsInRole(ForumRoles.Regular)
                    || httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != userId)
                    return Results.Forbid();

                dbContext.Remove(algorithmStatistic);
                await dbContext.SaveChangesAsync();

                return Results.NoContent();
            });
        }

        public static void GetPlayerEndpoints(RouteGroupBuilder playersGroup)
        {
            playersGroup.MapGet("players", async (int teamId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var team = await dbContext.Teams.FirstOrDefaultAsync(u => u.Id == teamId);
                if (team == null)
                    return Results.NotFound();

                var players = (await dbContext.Players.ToListAsync(cancellationToken))
                    .Where(o => o.CurrentTeam == team)
                    .Select(o => new PlayerDto(o.Id, o.Name, (int)o.Role, teamId));

                return Results.Ok(players);
            });

            playersGroup.MapGet("players/{playerId}", async (int teamId, int playerId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var team = await dbContext.Teams.FirstOrDefaultAsync(u => u.Id == teamId);
                if (team == null)
                    return Results.NotFound();

                var player = await dbContext.Players.FirstOrDefaultAsync(r => r.Id == playerId && r.CurrentTeam.Id == teamId);
                if (player == null)
                    return Results.NotFound();

                return Results.Ok(player);
            });

            playersGroup.MapPost("players", [Authorize(Roles = ForumRoles.Moderator)] async (int teamId, HttpContext httpContext, [Validate] CreatePlayerDto createPlayerDto, ForumDbContext dbContext) =>
            {
                var team = await dbContext.Teams.FirstOrDefaultAsync(u => u.Id == teamId);
                if (team == null)
                    return Results.NotFound();

                if (!httpContext.User.IsInRole(ForumRoles.Moderator))
                    return Results.Forbid();

                var player = new Player()
                {
                    Name = createPlayerDto.Name,
                    Role = (PlayerRole)createPlayerDto.Role,
                    CurrentTeam = team
                };

                dbContext.Players.Add(player);
                await dbContext.SaveChangesAsync();

                return Results.Created($"/api/Teams/{team.Id}/Players/{player.Id}", new PlayerDto(player.Id, player.Name, (int)player.Role, player.CurrentTeam.Id));
            });

            playersGroup.MapPut("players/{playerId}", [Authorize(Roles = ForumRoles.Moderator)] async (int teamId, int playerId, HttpContext httpContext, [Validate] UpdatePlayerDto updatePlayerDto, ForumDbContext dbContext) =>
            {
                var team = await dbContext.Teams.FirstOrDefaultAsync(u => u.Id == teamId);
                if (team == null)
                    return Results.NotFound();

                var player = await dbContext.Players.FirstOrDefaultAsync(r => r.Id == playerId && r.CurrentTeam.Id == teamId);
                if (player == null)
                    return Results.NotFound();

                if (!httpContext.User.IsInRole(ForumRoles.Moderator))
                    return Results.Forbid();

                player.Name = updatePlayerDto.Name;
                player.Role = (PlayerRole)updatePlayerDto.Role;

                dbContext.Update(player);
                await dbContext.SaveChangesAsync();

                return Results.Ok(player);
            });

            playersGroup.MapDelete("players/{playerId}", [Authorize(Roles = ForumRoles.Moderator)] async (int teamId, int playerId, HttpContext httpContext, ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var team = await dbContext.Teams.FirstOrDefaultAsync(u => u.Id == teamId);
                if (team == null)
                    return Results.NotFound();

                var player = await dbContext.Players.FirstOrDefaultAsync(r => r.Id == playerId && r.CurrentTeam.Id == teamId);
                if (player == null)
                    return Results.NotFound();

                if (!httpContext.User.IsInRole(ForumRoles.Moderator))
                    return Results.Forbid();

                dbContext.Remove(player);
                await dbContext.SaveChangesAsync();

                return Results.NoContent();
            });
        }

        public static void GetPlayerStatisticEndpoints(RouteGroupBuilder playerStatsGroup)
        {
            playerStatsGroup.MapGet("playerStatistics", async (int teamId, int playerId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var team = await dbContext.Teams.FirstOrDefaultAsync(u => u.Id == teamId);
                if (team == null)
                    return Results.NotFound();

                var player = await dbContext.Players.FirstOrDefaultAsync(r => r.Id == playerId && r.CurrentTeam.Id == teamId);
                if (player == null)
                    return Results.NotFound();

                await dbContext.Statistics.ToListAsync(cancellationToken);

                var playerStatistics = (await dbContext.PlayerStatistics.ToListAsync(cancellationToken))
                    .Where(o => o.Player == player)
                    .Select(o => new PlayerStatisticDto(
                        o.Id,
                        o.Value,
                        o.Type.Id,
                        playerId));

                return Results.Ok(playerStatistics);
            });

            playerStatsGroup.MapGet("playerStatistics/{playerStatisticId}", async (int teamId, int playerId, int playerStatisticId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var team = await dbContext.Teams.FirstOrDefaultAsync(u => u.Id == teamId);
                if (team == null)
                    return Results.NotFound();

                var player = await dbContext.Players.FirstOrDefaultAsync(r => r.Id == playerId && r.CurrentTeam.Id == teamId);
                if (player == null)
                    return Results.NotFound();

                var playerStatistic = await dbContext.PlayerStatistics.FirstOrDefaultAsync(r => r.Id == playerStatisticId && r.Player.Id == playerId);
                if (playerStatistic == null)
                    return Results.NotFound();

                await dbContext.Statistics.ToListAsync(cancellationToken);

                return Results.Ok(new PlayerStatisticDto(playerStatistic.Id, playerStatistic.Value, playerStatistic.Type.Id, playerId));
            });

            playerStatsGroup.MapPost("playerStatistics", [Authorize(Roles = ForumRoles.Moderator)] async (int teamId, int playerId, HttpContext httpContext, [Validate] CreatePlayerStatisticDto createPlayerStatisticDto, ForumDbContext dbContext) =>
            {
                var team = await dbContext.Teams.FirstOrDefaultAsync(u => u.Id == teamId);
                if (team == null)
                    return Results.NotFound();

                var player = await dbContext.Players.FirstOrDefaultAsync(r => r.Id == playerId && r.CurrentTeam.Id == teamId);
                if (player == null)
                    return Results.NotFound();

                if (!httpContext.User.IsInRole(ForumRoles.Moderator))
                    return Results.Forbid();

                var statistic = await dbContext.Statistics.FirstOrDefaultAsync(r => r.Id == createPlayerStatisticDto.StatisticId);
                if (statistic == null)
                    return Results.NotFound();

                var playerStatistic = new PlayerStatistic()
                {
                    Value = createPlayerStatisticDto.Value,
                    Player = player,
                    Type = statistic
                };

                dbContext.PlayerStatistics.Add(playerStatistic);
                await dbContext.SaveChangesAsync();

                return Results.Created($"/api/Teams/{team.Id}/Players/{player.Id}/PlayerStatistics/{playerStatistic.Id}",
                    new PlayerStatisticDto(playerStatistic.Id, playerStatistic.Value, playerStatistic.Type.Id, playerId));
            });

            playerStatsGroup.MapPut("playerStatistics/{playerStatisticId}", [Authorize(Roles = ForumRoles.Moderator)] async (int teamId, int playerId, int playerStatisticId, HttpContext httpContext, [Validate] UpdatePlayerStatisticDto updatePlayerStatisticDto, ForumDbContext dbContext) =>
            {
                var team = await dbContext.Teams.FirstOrDefaultAsync(u => u.Id == teamId);
                if (team == null)
                    return Results.NotFound();

                var player = await dbContext.Players.FirstOrDefaultAsync(r => r.Id == playerId && r.CurrentTeam.Id == teamId);
                if (player == null)
                    return Results.NotFound();

                var playerStatistic = await dbContext.PlayerStatistics.FirstOrDefaultAsync(r => r.Id == playerStatisticId && r.Player.Id == playerId);
                if (playerStatistic == null)
                    return Results.NotFound();

                if (!httpContext.User.IsInRole(ForumRoles.Moderator))
                    return Results.Forbid();

                var statistic = await dbContext.Statistics.FirstOrDefaultAsync(r => r.Id == updatePlayerStatisticDto.StatisticId);
                if (statistic == null)
                    return Results.NotFound();

                playerStatistic.Type = statistic;
                playerStatistic.Value = updatePlayerStatisticDto.Value;

                dbContext.Update(playerStatistic);
                await dbContext.SaveChangesAsync();

                return Results.Ok(new PlayerStatisticDto(playerStatistic.Id, playerStatistic.Value, playerStatistic.Type.Id, playerId));
            });

            playerStatsGroup.MapDelete("playerStatistics/{playerStatisticId}", [Authorize(Roles = ForumRoles.Moderator)] async (int teamId, int playerId, int playerStatisticId, HttpContext httpContext, ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var team = await dbContext.Teams.FirstOrDefaultAsync(u => u.Id == teamId);
                if (team == null)
                    return Results.NotFound();

                var player = await dbContext.Players.FirstOrDefaultAsync(r => r.Id == playerId && r.CurrentTeam.Id == teamId);
                if (player == null)
                    return Results.NotFound();

                var playerStatistic = await dbContext.PlayerStatistics.FirstOrDefaultAsync(r => r.Id == playerStatisticId && r.Player.Id == playerId);
                if (playerStatistic == null)
                    return Results.NotFound();

                if (!httpContext.User.IsInRole(ForumRoles.Moderator))
                    return Results.Forbid();

                dbContext.Remove(playerStatistic);
                await dbContext.SaveChangesAsync();

                return Results.NoContent();
            });
        }

        public static void GetAlgorithmImpressionEndpoints(RouteGroupBuilder algoImpressionsStatsGroup)
        {
            algoImpressionsStatsGroup.MapGet("algorithmImpressions", async (string userId, UserManager<ForumRestUser> userManager, int ratingAlgorithmId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.User.Id == userId);
                if (ratingAlgorithm == null)
                    return Results.NotFound();


                var algorithmImpressions = (await dbContext.AlgorithmImpressions.ToListAsync(cancellationToken))
                    .Where(o => o.RatingAlgorithm == ratingAlgorithm)
                    .Select(o => new AlgorithmImpressionDto(
                        o.Id,
                        o.Positive,
                        ratingAlgorithmId,
                        o.UserId));

                return Results.Ok(algorithmImpressions);
            });

            algoImpressionsStatsGroup.MapGet("algorithmImpressions/{algorithmImpressionId}", async (string userId, UserManager<ForumRestUser> userManager, int ratingAlgorithmId, int algorithmImpressionId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.User.Id == userId);
                if (ratingAlgorithm == null)
                    return Results.NotFound();

                var algorithmImpression = await dbContext.AlgorithmImpressions.FirstOrDefaultAsync(r => r.Id == algorithmImpressionId && r.RatingAlgorithm.Id == ratingAlgorithmId);
                if (algorithmImpression == null)
                    return Results.NotFound();


                return Results.Ok(new AlgorithmImpressionDto(algorithmImpression.Id, algorithmImpression.Positive, ratingAlgorithmId, algorithmImpression.UserId));
            });

            algoImpressionsStatsGroup.MapPost("algorithmImpressions", [Authorize(Roles = ForumRoles.Regular)] async (string userId, UserManager<ForumRestUser> userManager, HttpContext httpContext, int ratingAlgorithmId, [Validate] CreateAlgorithmImpressionDto createAlgorithmImpressionDto, ForumDbContext dbContext) =>
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.User.Id == userId);
                if (ratingAlgorithm == null)
                    return Results.NotFound();

                var impressionUser = await dbContext.Users.FirstOrDefaultAsync(r => r.Id == httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub));
                if (impressionUser == null)
                    return Results.NotFound();

                if (!httpContext.User.IsInRole(ForumRoles.Regular))
                    return Results.Forbid();

                var existingImpression = await dbContext.AlgorithmImpressions.FirstOrDefaultAsync(i => i.RatingAlgorithm.Id == ratingAlgorithm.Id && i.UserId == impressionUser.Id);
                if (existingImpression != null)
                {
                    return Results.UnprocessableEntity("User has already rated this algorithm");
                }

                var algorithmImpression = new AlgorithmImpression()
                {
                    Positive = createAlgorithmImpressionDto.Positive,
                    RatingAlgorithm = ratingAlgorithm,
                    UserId = impressionUser.Id
                };

                dbContext.AlgorithmImpressions.Add(algorithmImpression);
                await dbContext.SaveChangesAsync();

                return Results.Created($"/api/Users/{user.Id}/RatingAlgorithms/{ratingAlgorithm.Id}/AlgorithmImpressions/{algorithmImpression.Id}",
                    new AlgorithmImpressionDto(algorithmImpression.Id, algorithmImpression.Positive, ratingAlgorithmId, algorithmImpression.UserId));
            });

            algoImpressionsStatsGroup.MapPut("algorithmImpressions/{algorithmImpressionId}", [Authorize(Roles = ForumRoles.Regular)] async (string userId, UserManager<ForumRestUser> userManager, HttpContext httpContext, int ratingAlgorithmId, int algorithmImpressionId, [Validate] UpdateAlgorithmImpressionDto updateAlgorithmImpressionDto, ForumDbContext dbContext) =>
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.User.Id == userId);
                if (ratingAlgorithm == null)
                    return Results.NotFound();

                var algorithmImpression = await dbContext.AlgorithmImpressions.FirstOrDefaultAsync(r => r.Id == algorithmImpressionId && r.RatingAlgorithm.Id == ratingAlgorithmId);
                if (algorithmImpression == null)
                    return Results.NotFound();

                var impressionUser = await dbContext.Users.FirstOrDefaultAsync(r => r.Id == httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub));
                if (impressionUser == null)
                    return Results.NotFound();

                if (!httpContext.User.IsInRole(ForumRoles.Regular)
                    || impressionUser.Id != algorithmImpression.UserId)
                    return Results.Forbid();

                algorithmImpression.Positive = updateAlgorithmImpressionDto.Positive;

                dbContext.Update(algorithmImpression);
                await dbContext.SaveChangesAsync();

                return Results.Ok(new AlgorithmImpressionDto(algorithmImpression.Id, algorithmImpression.Positive, ratingAlgorithmId, algorithmImpression.UserId));
            });

            algoImpressionsStatsGroup.MapDelete("algorithmImpressions/{algorithmImpressionId}", [Authorize(Roles = ForumRoles.Regular)] async (string userId, UserManager<ForumRestUser> userManager, HttpContext httpContext, int ratingAlgorithmId, int algorithmImpressionId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.User.Id == userId);
                if (ratingAlgorithm == null)
                    return Results.NotFound();

                var algorithmImpression = await dbContext.AlgorithmImpressions.FirstOrDefaultAsync(r => r.Id == algorithmImpressionId && r.RatingAlgorithm.Id == ratingAlgorithmId);
                if (algorithmImpression == null)
                    return Results.NotFound();

                if (!httpContext.User.IsInRole(ForumRoles.Regular)
                    || httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != algorithmImpression.UserId)
                    return Results.Forbid();

                dbContext.Remove(algorithmImpression);
                await dbContext.SaveChangesAsync();

                return Results.NoContent();
            });
        }
    }
}

public record UserDto(string Id, string Username, string Email, IList<string> roles);
public record UserWithoutRolesDto(string Id, string Username, string Email);
public record CreateUserDto(string Username, string Password, string Email, int Role);
public record UpdateUserDto(string OldPassword, string NewPassword, string Email, int Role);
public record CreateRatingAlgorithmDto(string Formula, bool Promoted);
public record UpdateRatingAlgorithmDto(string Formula, bool Promoted);
public record CreateStatisticDto(string Name, string DisplayName, Visibility? Status);
public record UpdateStatisticDto(string Name, string DisplayName, Visibility? Status);
public record CreateTeamDto(string Name);
public record UpdateTeamDto(string Name);
public record CreateAlgorithmStatisticDto(int? StatisticId);
public record UpdateAlgorithmStatisticDto(int? StatisticId);
public record CreatePlayerStatisticDto(float Value, int? StatisticId);
public record UpdatePlayerStatisticDto(float Value, int? StatisticId);
public record CreateAlgorithmImpressionDto(bool Positive);
public record UpdateAlgorithmImpressionDto(bool Positive);
public record CreatePlayerDto(string Name, PlayerRole? Role);
public record UpdatePlayerDto(string Name, PlayerRole? Role);

public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleFor(dto => dto.Username).NotEmpty().NotNull().Length(min: 3, max: 20);
        RuleFor(dto => dto.Password).NotEmpty().NotNull().Length(min: 3, max: 80);
        RuleFor(dto => dto.Email).NotEmpty().NotNull().EmailAddress().Length(min: 3, max: 80);
        RuleFor(dto => (int?)dto.Role).NotNull().NotEmpty().GreaterThanOrEqualTo(0).LessThanOrEqualTo(15);
    }
}

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        RuleFor(dto => dto.OldPassword).NotEmpty().NotNull().Length(min: 3, max: 80);
        RuleFor(dto => dto.NewPassword).NotEmpty().NotNull().Length(min: 3, max: 80);
        RuleFor(dto => dto.Email).NotEmpty().NotNull().EmailAddress().Length(min: 3, max: 80);
        RuleFor(dto => (int?)dto.Role).NotEmpty().NotNull().GreaterThanOrEqualTo(0).LessThanOrEqualTo(15);
    }
}

public class CreateRatingAlgorithmDtoValidator : AbstractValidator<CreateRatingAlgorithmDto>
{
    public CreateRatingAlgorithmDtoValidator()
    {
        // TODO: make API method where algorithmStats are created based on formula
        RuleFor(dto => dto.Formula).NotEmpty().NotNull()
            .Matches(@"^\([0-9]+(\s[0-9]+)*\)\s[^\(\)]*(((?'Open'\()[^\(\)]*)+((?'Close-Open'\))[^\(\)]*)+)*(?(Open)(?!))$")
            .Length(min: 3, max: 500);
        RuleFor(dto => dto.Promoted).NotNull();
    }
}

public class UpdateRatingAlgorithmDtoValidator : AbstractValidator<UpdateRatingAlgorithmDto>
{
    public UpdateRatingAlgorithmDtoValidator()
    {
        RuleFor(dto => dto.Formula).NotEmpty().NotNull()
            .Matches(@"^\([0-9]+(\s[0-9]+)*\)\s[^\(\)]*(((?'Open'\()[^\(\)]*)+((?'Close-Open'\))[^\(\)]*)+)*(?(Open)(?!))$")
            .Length(min: 3, max: 500);
        RuleFor(dto => dto.Promoted).NotNull();
    }
}

public class CreateStatisticDtoValidator : AbstractValidator<CreateStatisticDto>
{
    public CreateStatisticDtoValidator()
    {
        RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 3, max: 30);
        RuleFor(dto => dto.DisplayName).NotEmpty().NotNull().Length(min: 3, max: 80);
        RuleFor(dto => (int?)dto.Status).NotEmpty().NotNull().GreaterThanOrEqualTo(0).LessThanOrEqualTo(1);
    }
}

public class UpdateStatisticDtoValidator : AbstractValidator<UpdateStatisticDto>
{
    public UpdateStatisticDtoValidator()
    {
        RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 3, max: 30);
        RuleFor(dto => dto.DisplayName).NotEmpty().NotNull().Length(min: 3, max: 80);
        RuleFor(dto => (int?)dto.Status).NotEmpty().NotNull().GreaterThanOrEqualTo(0).LessThanOrEqualTo(1);
    }
}

public class CreateTeamDtoValidator : AbstractValidator<CreateTeamDto>
{
    public CreateTeamDtoValidator()
    {
        RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 3, max: 40);
    }
}

public class UpdateTeamDtoValidator : AbstractValidator<UpdateTeamDto>
{
    public UpdateTeamDtoValidator()
    {
        RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 3, max: 40);
    }
}

public class CreateAlgorithmStatisticDtoValidator : AbstractValidator<CreateAlgorithmStatisticDto>
{
    public CreateAlgorithmStatisticDtoValidator()
    {
        RuleFor(dto => dto.StatisticId).NotEmpty().NotNull().GreaterThan(-1);
    }
}

public class UpdateAlgorithmStatisticDtoValidator : AbstractValidator<UpdateAlgorithmStatisticDto>
{
    public UpdateAlgorithmStatisticDtoValidator()
    {
        RuleFor(dto => dto.StatisticId).NotEmpty().NotNull().GreaterThan(-1);
    }
}

public class CreatePlayerStatisticDtoValidator : AbstractValidator<CreatePlayerStatisticDto>
{
    public CreatePlayerStatisticDtoValidator()
    {
        RuleFor(dto => dto.StatisticId).NotEmpty().NotNull().GreaterThan(-1);
        RuleFor(dto => dto.Value).NotEmpty().NotNull();
    }
}

public class UpdatePlayerStatisticDtoValidator : AbstractValidator<UpdatePlayerStatisticDto>
{
    public UpdatePlayerStatisticDtoValidator()
    {
        RuleFor(dto => dto.StatisticId).NotEmpty().NotNull().GreaterThan(-1);
        RuleFor(dto => dto.Value).NotEmpty().NotNull();
    }
}

public class CreateAlgorithmImpressionDtoValidator : AbstractValidator<CreateAlgorithmImpressionDto>
{
    public CreateAlgorithmImpressionDtoValidator()
    {
        RuleFor(dto => dto.Positive).NotNull();
    }
}

public class UpdateAlgorithmImpressionDtoValidator : AbstractValidator<UpdateAlgorithmImpressionDto>
{
    public UpdateAlgorithmImpressionDtoValidator()
    {
        RuleFor(dto => dto.Positive).NotNull();
    }
}

public class CreatePlayerDtoValidator : AbstractValidator<CreatePlayerDto>
{
    public CreatePlayerDtoValidator()
    {
        RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 3, max: 40);
        RuleFor(dto => (int?)dto.Role).NotEmpty().NotNull().GreaterThanOrEqualTo(0).LessThanOrEqualTo(4);
    }
}

public class UpdatePlayerDtoValidator : AbstractValidator<UpdatePlayerDto>
{
    public UpdatePlayerDtoValidator()
    {
        RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 3, max: 40);
        RuleFor(dto => (int?)dto.Role).NotEmpty().NotNull().GreaterThanOrEqualTo(0).LessThanOrEqualTo(4);
    }
}
