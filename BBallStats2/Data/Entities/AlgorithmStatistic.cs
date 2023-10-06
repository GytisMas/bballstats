namespace BBallStats.Data.Entities
{
	public class AlgorithmStatistic
    {
        public int Id { get; set; }
		public required Statistic StatType { get; set; }
		public required RatingAlgorithm Algorithm { get; set; }
        public record AlgorithmStatisticDto(int Id, int StatType, int Algorithm);
    }
}