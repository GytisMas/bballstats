using BBallStats.Data.Entities;
using BBallStats.Data;
using O9d.AspNet.FluentValidation;
using Microsoft.EntityFrameworkCore;
using static BBallStats.Data.Entities.User;
using static BBallStats.Data.Entities.RatingAlgorithm;
using static BBallStats.Data.Entities.AlgorithmStatistic;
using static BBallStats.Data.Entities.Team;
using static BBallStats.Data.Entities.Player;
using static BBallStats.Data.Entities.PlayerStatistic;
using static BBallStats.Data.Entities.Statistic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace BBallStats2
{
    public static class Endpoints
    {
        public static void GetUserEndpoints(RouteGroupBuilder usersGroup)
        {
            usersGroup.MapGet("users", async (ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                return (await dbContext.Users.ToListAsync(cancellationToken))
                    .Select(o => new UserDto(o.Id, o.Username, o.Password, o.Email, (int)o.Type));
            });

            usersGroup.MapGet("users/{userId}", async (int userId, ForumDbContext dbContext) =>
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return Results.NotFound();

                return Results.Ok(new UserDto(user.Id, user.Username, user.Password, user.Email, ((int)user.Type)));
            });

            usersGroup.MapPost("users", async ([Validate] CreateUserDto createUserDto, ForumDbContext dbContext) =>
            {
                var user = new User()
                {
                    Username = createUserDto.Username,
                    Password = createUserDto.Password,
                    Email = createUserDto.Email,
                    Type = createUserDto.Type
                };

                dbContext.Users.Add(user);
                await dbContext.SaveChangesAsync();

                return Results.Created($"/api/Users/{user.Id}", new UserDto(user.Id, user.Username, user.Password, user.Email, ((int)user.Type)));
            });

            usersGroup.MapPut("users/{userId}", async (int userId, [Validate] UpdateUserDto updateUserDto, ForumDbContext dbContext) =>
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return Results.NotFound();

                user.Password = updateUserDto.Password;
                user.Email = updateUserDto.Email;
                user.Type = updateUserDto.Type;

                dbContext.Update(user);
                await dbContext.SaveChangesAsync();

                return Results.Ok(new UserDto(user.Id, user.Username, user.Password, user.Email, ((int)user.Type)));
            });

            usersGroup.MapDelete("users/{userId}", async (int userId, ForumDbContext dbContext) =>
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return Results.NotFound();

                dbContext.Remove(user);
                await dbContext.SaveChangesAsync();

                return Results.NoContent();
            });
        }

        public static void GetRatingAlgorithmEndpoints(RouteGroupBuilder ratingsGroup)
        {
            ratingsGroup.MapGet("ratingAlgorithms", async (int userId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithms = (await dbContext.RatingAlgorithms.ToListAsync(cancellationToken))
                    .Where(o => o.Author == user)
                    .Select(o => new RatingAlgorithmDto(o.Id, o.Formula, userId));

                return Results.Ok(ratingAlgorithms);
            });

            ratingsGroup.MapGet("ratingAlgorithms/{ratingAlgorithmId}", async (int userId, int ratingAlgorithmId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.Author.Id == userId);
                if (ratingAlgorithm == null)
                    return Results.NotFound();

                return Results.Ok(ratingAlgorithm);
            });

            ratingsGroup.MapPost("ratingAlgorithms", async (int userId, [Validate] CreateRatingAlgorithmDto createRatingAlgorithmDto, ForumDbContext dbContext) =>
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = new RatingAlgorithm()
                {
                    Formula = createRatingAlgorithmDto.Formula,
                    Author = user
                };

                dbContext.RatingAlgorithms.Add(ratingAlgorithm);
                await dbContext.SaveChangesAsync();

                return Results.Created($"/api/Users/{user.Id}/RatingAlgorithms/{ratingAlgorithm.Id}", new RatingAlgorithmDto(ratingAlgorithm.Id, ratingAlgorithm.Formula, ratingAlgorithm.Author.Id));
            });

            ratingsGroup.MapPut("ratingAlgorithms/{ratingAlgorithmId}", async (int userId, int ratingAlgorithmId, [Validate] UpdateRatingAlgorithmDto updateRatingAlgorithmDto, ForumDbContext dbContext) =>
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.Author.Id == userId);
                if (ratingAlgorithm == null)
                    return Results.NotFound();

                ratingAlgorithm.Formula = updateRatingAlgorithmDto.Formula;

                dbContext.Update(ratingAlgorithm);
                await dbContext.SaveChangesAsync();

                return Results.Ok(ratingAlgorithm);
            });

            ratingsGroup.MapDelete("ratingAlgorithms/{ratingAlgorithmId}", async (int userId, int ratingAlgorithmId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.Author.Id == userId);
                if (ratingAlgorithm == null)
                    return Results.NotFound();

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

            statisticsGroup.MapGet("statistics/{statisticId}", async (int statisticId, ForumDbContext dbContext) =>
            {
                var statistic = await dbContext.Statistics.FirstOrDefaultAsync(u => u.Id == statisticId);
                if (statistic == null)
                    return Results.NotFound();

                return Results.Ok(new StatisticDto(statistic.Id, statistic.Name, statistic.DisplayName, (int)statistic.Status));
            });

            statisticsGroup.MapPost("statistics", async ([Validate] CreateStatisticDto createStatisticDto, ForumDbContext dbContext) =>
            {
                var statistic = new Statistic()
                {
                    Name = createStatisticDto.Name,
                    DisplayName = createStatisticDto.DisplayName,
                    Status = createStatisticDto.Status
                };

                dbContext.Statistics.Add(statistic);
                await dbContext.SaveChangesAsync();

                return Results.Created($"/api/statistics/{statistic.Id}", new StatisticDto(statistic.Id, statistic.Name, statistic.DisplayName, (int)statistic.Status));
            });

            statisticsGroup.MapPut("statistics/{statisticId}", async (int statisticId, [Validate] UpdateStatisticDto updateStatisticDto, ForumDbContext dbContext) =>
            {
                var statistic = await dbContext.Statistics.FirstOrDefaultAsync(u => u.Id == statisticId);
                if (statistic == null)
                    return Results.NotFound();

                statistic.Name = updateStatisticDto.Name;
                statistic.DisplayName = updateStatisticDto.DisplayName;
                statistic.Status = updateStatisticDto.Status;

                dbContext.Update(statistic);
                await dbContext.SaveChangesAsync();

                return Results.Ok(new StatisticDto(statistic.Id, statistic.Name, statistic.DisplayName, (int)statistic.Status));
            });

            statisticsGroup.MapDelete("statistics/{statisticId}", async (int statisticId, ForumDbContext dbContext) =>
            {
                var statistic = await dbContext.Statistics.FirstOrDefaultAsync(u => u.Id == statisticId);
                if (statistic == null)
                    return Results.NotFound();

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

            teamsGroup.MapPost("teams", async ([Validate] CreateTeamDto createTeamDto, ForumDbContext dbContext) =>
            {
                var team = new Team()
                {
                    Name = createTeamDto.Name
                };

                dbContext.Teams.Add(team);
                await dbContext.SaveChangesAsync();

                return Results.Created($"/api/Teams/{team.Id}", team);
            });

            teamsGroup.MapPut("teams/{teamId}", async (int teamId, [Validate] UpdateTeamDto updateTeamDto, ForumDbContext dbContext) =>
            {
                var team = await dbContext.Teams.FirstOrDefaultAsync(u => u.Id == teamId);
                if (team == null)
                    return Results.NotFound();

                team.Name = updateTeamDto.Name;

                dbContext.Update(team);
                await dbContext.SaveChangesAsync();

                return Results.Ok(team);
            });

            teamsGroup.MapDelete("teams/{teamId}", async (int teamId, ForumDbContext dbContext) =>
            {
                var team = await dbContext.Teams.FirstOrDefaultAsync(u => u.Id == teamId);
                if (team == null)
                    return Results.NotFound();

                dbContext.Remove(team);
                await dbContext.SaveChangesAsync();

                return Results.NoContent();
            });
        }

        public static void GetAlgorithmStatisticEndpoints(RouteGroupBuilder algoStatsGroup)
        {
            algoStatsGroup.MapGet("algorithmStatistics", async (int userId, int ratingAlgorithmId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.Author.Id == userId);
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

            algoStatsGroup.MapGet("algorithmStatistics/{algorithmStatisticId}", async (int userId, int ratingAlgorithmId, int algorithmStatisticId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.Author.Id == userId);
                if (ratingAlgorithm == null)
                    return Results.NotFound();

                var algorithmStatistic = await dbContext.AlgorithmStatistics.FirstOrDefaultAsync(r => r.Id == algorithmStatisticId && r.Algorithm.Id == ratingAlgorithmId);
                if (algorithmStatistic == null)
                    return Results.NotFound();

                await dbContext.Statistics.ToListAsync(cancellationToken);

                return Results.Ok(new AlgorithmStatisticDto(algorithmStatistic.Id, algorithmStatistic.StatType.Id, ratingAlgorithmId));
            });

            algoStatsGroup.MapPost("algorithmStatistics", async (int userId, int ratingAlgorithmId, [Validate] CreateAlgorithmStatisticDto createAlgorithmStatisticDto, ForumDbContext dbContext) =>
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.Author.Id == userId);
                if (ratingAlgorithm == null)
                    return Results.NotFound();

                var statistic = await dbContext.Statistics.FirstOrDefaultAsync(r => r.Id == createAlgorithmStatisticDto.statisticId);
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

            algoStatsGroup.MapPut("algorithmStatistics/{algorithmStatisticId}", async (int userId, int ratingAlgorithmId, int algorithmStatisticId, [Validate] UpdateAlgorithmStatisticDto updateAlgorithmStatisticDto, ForumDbContext dbContext) =>
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.Author.Id == userId);
                if (ratingAlgorithm == null)
                    return Results.NotFound();

                var algorithmStatistic = await dbContext.AlgorithmStatistics.FirstOrDefaultAsync(r => r.Id == algorithmStatisticId && r.Algorithm.Id == ratingAlgorithmId);
                if (algorithmStatistic == null)
                    return Results.NotFound();

                var statistic = await dbContext.Statistics.FirstOrDefaultAsync(r => r.Id == updateAlgorithmStatisticDto.statisticId);
                if (statistic == null)
                    return Results.NotFound();

                algorithmStatistic.StatType = statistic;

                dbContext.Update(algorithmStatistic);
                await dbContext.SaveChangesAsync();

                return Results.Ok(new AlgorithmStatisticDto(algorithmStatistic.Id, algorithmStatistic.StatType.Id, ratingAlgorithmId));
            });

            algoStatsGroup.MapDelete("algorithmStatistics/{algorithmStatisticId}", async (int userId, int ratingAlgorithmId, int algorithmStatisticId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.Author.Id == userId);
                if (ratingAlgorithm == null)
                    return Results.NotFound();

                var algorithmStatistic = await dbContext.AlgorithmStatistics.FirstOrDefaultAsync(r => r.Id == algorithmStatisticId && r.Algorithm.Id == ratingAlgorithmId);
                if (algorithmStatistic == null)
                    return Results.NotFound();

                dbContext.Remove(algorithmStatistic);
                await dbContext.SaveChangesAsync();

                return Results.NoContent();
            });
        }
    }
}
