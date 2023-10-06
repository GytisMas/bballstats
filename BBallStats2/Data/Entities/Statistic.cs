namespace BBallStats.Data.Entities
{
	public enum Visibility
	{
		Hidden = 0,
		Public = 1
	}

	public class Statistic
    {
        public int Id { get; set; }
        public required string Name { get; set; }
		public required string DisplayName { get; set; }
		public required Visibility Status { get; set; }
        public record StatisticDto(int Id, string Name, string DisplayName, int Status);
    }
}