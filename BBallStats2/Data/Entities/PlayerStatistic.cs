namespace BBallStats.Data.Entities
{
    public class PlayerStatistic
    {
        public int Id { get; set; }
        public required float Value { get; set; }

        public required PlayerStatistic Type { get; set; }
        public required Player Player { get; set; }
    }
}
