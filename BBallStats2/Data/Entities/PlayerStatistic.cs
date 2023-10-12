namespace BBallStats.Data.Entities
{
    public class PlayerStatistic
    {
        public int Id { get; set; }
        public required float Value { get; set; }

        public required Statistic Type { get; set; }
        public required Player Player { get; set; }

        public record PlayerStatisticDto(int Id, float value, int StatType, int Player);

    }
}
