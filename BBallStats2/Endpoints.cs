using BBallStats.Data.Entities;
using BBallStats.Data;
using O9d.AspNet.FluentValidation;
using Microsoft.EntityFrameworkCore;
using static BBallStats.Data.Entities.User;
using static BBallStats.Data.Entities.RatingAlgorithm;
using static BBallStats.Data.Entities.AlgorithmStatistic;
using static BBallStats.Data.Entities.AlgorithmImpression;
using static BBallStats.Data.Entities.Team;
using static BBallStats.Data.Entities.Player;
using static BBallStats.Data.Entities.PlayerStatistic;
using static BBallStats.Data.Entities.Statistic;
using System.Threading;

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
                    Type = (UserType)createUserDto.Type
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
                user.Type = (UserType)updateUserDto.Type;

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
                    .Select(o => new RatingAlgorithmDto(o.Id, o.Formula, o.Promoted, userId));

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

                List<int> formulaStatIds = createRatingAlgorithmDto.Formula.Split(") ")[0]
                    .Remove(0, 1)
                    .Split(" ", StringSplitOptions.RemoveEmptyEntries)
                    .Select(Int32.Parse).ToList<int>();

                var Stats = (await dbContext.Statistics.ToListAsync())
                    .Where(s => formulaStatIds.Contains(s.Id)).ToList();
                if (Stats.Count < formulaStatIds.Count)
                    return Results.NotFound();

                var ratingAlgorithm = new RatingAlgorithm()
                {
                    Formula = createRatingAlgorithmDto.Formula,
                    Promoted = createRatingAlgorithmDto.Promoted,
                    Author = user
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

                return Results.Created($"/api/Users/{user.Id}/RatingAlgorithms/{ratingAlgorithm.Id}", new RatingAlgorithmDto(ratingAlgorithm.Id, ratingAlgorithm.Formula, ratingAlgorithm.Promoted, ratingAlgorithm.Author.Id));
            });

            ratingsGroup.MapPut("ratingAlgorithms/{ratingAlgorithmId}", async (int userId, int ratingAlgorithmId, [Validate] UpdateRatingAlgorithmDto updateRatingAlgorithmDto, ForumDbContext dbContext) =>
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.Author.Id == userId);
                if (ratingAlgorithm == null)
                    return Results.NotFound();


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
                    Status = (Visibility)createStatisticDto.Status
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
                statistic.Status = (Visibility)updateStatisticDto.Status;

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

                var statistic = await dbContext.Statistics.FirstOrDefaultAsync(r => r.Id == updateAlgorithmStatisticDto.StatisticId);
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

            playersGroup.MapPost("players", async (int teamId, [Validate] CreatePlayerDto createPlayerDto, ForumDbContext dbContext) =>
            {
                var team = await dbContext.Teams.FirstOrDefaultAsync(u => u.Id == teamId);
                if (team == null)
                    return Results.NotFound();

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

            playersGroup.MapPut("players/{playerId}", async (int teamId, int playerId, [Validate] UpdatePlayerDto updatePlayerDto, ForumDbContext dbContext) =>
            {
                var team = await dbContext.Teams.FirstOrDefaultAsync(u => u.Id == teamId);
                if (team == null)
                    return Results.NotFound();

                var player = await dbContext.Players.FirstOrDefaultAsync(r => r.Id == playerId && r.CurrentTeam.Id == teamId);
                if (player == null)
                    return Results.NotFound();

                player.Name = updatePlayerDto.Name;
                player.Role = (PlayerRole)updatePlayerDto.Role;

                dbContext.Update(player);
                await dbContext.SaveChangesAsync();

                return Results.Ok(player);
            });

            playersGroup.MapDelete("players/{playerId}", async (int teamId, int playerId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var team = await dbContext.Teams.FirstOrDefaultAsync(u => u.Id == teamId);
                if (team == null)
                    return Results.NotFound();

                var player = await dbContext.Players.FirstOrDefaultAsync(r => r.Id == playerId && r.CurrentTeam.Id == teamId);
                if (player == null)
                    return Results.NotFound();

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

            playerStatsGroup.MapPost("playerStatistics", async (int teamId, int playerId, [Validate] CreatePlayerStatisticDto createPlayerStatisticDto, ForumDbContext dbContext) =>
            {
                var team = await dbContext.Teams.FirstOrDefaultAsync(u => u.Id == teamId);
                if (team == null)
                    return Results.NotFound();

                var player = await dbContext.Players.FirstOrDefaultAsync(r => r.Id == playerId && r.CurrentTeam.Id == teamId);
                if (player == null)
                    return Results.NotFound();

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

            playerStatsGroup.MapPut("playerStatistics/{playerStatisticId}", async (int teamId, int playerId, int playerStatisticId, [Validate] UpdatePlayerStatisticDto updatePlayerStatisticDto, ForumDbContext dbContext) =>
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

                var statistic = await dbContext.Statistics.FirstOrDefaultAsync(r => r.Id == updatePlayerStatisticDto.StatisticId);
                if (statistic == null)
                    return Results.NotFound();

                playerStatistic.Type = statistic;

                dbContext.Update(playerStatistic);
                await dbContext.SaveChangesAsync();

                return Results.Ok(new PlayerStatisticDto(playerStatistic.Id, playerStatistic.Value, playerStatistic.Type.Id, playerId));
            });

            playerStatsGroup.MapDelete("playerStatistics/{playerStatisticId}", async (int teamId, int playerId, int playerStatisticId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
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

                dbContext.Remove(playerStatistic);
                await dbContext.SaveChangesAsync();

                return Results.NoContent();
            });
        }

        public static void GetAlgorithmImpressionEndpoints(RouteGroupBuilder algoImpressionsStatsGroup)
        {
            algoImpressionsStatsGroup.MapGet("algorithmImpressions", async (int userId, int ratingAlgorithmId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.Author.Id == userId);
                if (ratingAlgorithm == null)
                    return Results.NotFound();

                await dbContext.Users.ToListAsync(cancellationToken);

                var algorithmImpressions = (await dbContext.AlgorithmImpressions.ToListAsync(cancellationToken))
                    .Where(o => o.RatingAlgorithm == ratingAlgorithm)
                    .Select(o => new AlgorithmImpressionDto(
                        o.Id,
                        o.Positive,
                        o.User.Id,
                        ratingAlgorithmId));

                return Results.Ok(algorithmImpressions);
            });

            algoImpressionsStatsGroup.MapGet("algorithmImpressions/{algorithmImpressionId}", async (int userId, int ratingAlgorithmId, int algorithmImpressionId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.Author.Id == userId);
                if (ratingAlgorithm == null)
                    return Results.NotFound();

                var algorithmImpression = await dbContext.AlgorithmImpressions.FirstOrDefaultAsync(r => r.Id == algorithmImpressionId && r.RatingAlgorithm.Id == ratingAlgorithmId);
                if (algorithmImpression == null)
                    return Results.NotFound();

                await dbContext.Users.ToListAsync(cancellationToken);

                return Results.Ok(new AlgorithmImpressionDto(algorithmImpression.Id, algorithmImpression.Positive, algorithmImpression.User.Id, ratingAlgorithmId));
            });

            algoImpressionsStatsGroup.MapPost("algorithmImpressions", async (int userId, int ratingAlgorithmId, [Validate] CreateAlgorithmImpressionDto createAlgorithmImpressionDto, ForumDbContext dbContext) =>
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.Author.Id == userId);
                if (ratingAlgorithm == null)
                    return Results.NotFound();

                var impressionUser = await dbContext.Users.FirstOrDefaultAsync(r => r.Id == createAlgorithmImpressionDto.UserId);
                if (impressionUser == null)
                    return Results.NotFound();

                var algorithmImpression = new AlgorithmImpression()
                {
                    Positive = createAlgorithmImpressionDto.Positive,
                    RatingAlgorithm = ratingAlgorithm,
                    User = impressionUser
                };

                dbContext.AlgorithmImpressions.Add(algorithmImpression);
                await dbContext.SaveChangesAsync();

                return Results.Created($"/api/Users/{user.Id}/RatingAlgorithms/{ratingAlgorithm.Id}/AlgorithmImpressions/{algorithmImpression.Id}",
                    new AlgorithmImpressionDto(algorithmImpression.Id, algorithmImpression.Positive, algorithmImpression.User.Id, ratingAlgorithmId));
            });

            algoImpressionsStatsGroup.MapPut("algorithmImpressions/{algorithmImpressionId}", async (int userId, int ratingAlgorithmId, int algorithmImpressionId, [Validate] UpdateAlgorithmImpressionDto updateAlgorithmImpressionDto, ForumDbContext dbContext) =>
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.Author.Id == userId);
                if (ratingAlgorithm == null)
                    return Results.NotFound();

                var algorithmImpression = await dbContext.AlgorithmImpressions.FirstOrDefaultAsync(r => r.Id == algorithmImpressionId && r.RatingAlgorithm.Id == ratingAlgorithmId);
                if (algorithmImpression == null)
                    return Results.NotFound();

                var impressionUser = await dbContext.Users.FirstOrDefaultAsync(r => r.Id == updateAlgorithmImpressionDto.UserId);
                if (impressionUser == null)
                    return Results.NotFound();

                algorithmImpression.User = impressionUser;

                dbContext.Update(algorithmImpression);
                await dbContext.SaveChangesAsync();

                return Results.Ok(new AlgorithmImpressionDto(algorithmImpression.Id, algorithmImpression.Positive, algorithmImpression.User.Id, ratingAlgorithmId));
            });

            algoImpressionsStatsGroup.MapDelete("algorithmImpressions/{algorithmImpressionId}", async (int userId, int ratingAlgorithmId, int algorithmImpressionId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return Results.NotFound();

                var ratingAlgorithm = await dbContext.RatingAlgorithms.FirstOrDefaultAsync(r => r.Id == ratingAlgorithmId && r.Author.Id == userId);
                if (ratingAlgorithm == null)
                    return Results.NotFound();

                var algorithmImpression = await dbContext.AlgorithmImpressions.FirstOrDefaultAsync(r => r.Id == algorithmImpressionId && r.RatingAlgorithm.Id == ratingAlgorithmId);
                if (algorithmImpression == null)
                    return Results.NotFound();

                dbContext.Remove(algorithmImpression);
                await dbContext.SaveChangesAsync();

                return Results.NoContent();
            });
        }
    }
}
