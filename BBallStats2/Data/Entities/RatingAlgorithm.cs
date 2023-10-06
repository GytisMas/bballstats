namespace BBallStats.Data.Entities
{
	public class RatingAlgorithm
	{
		public int Id { get; set; }
		public required string Formula { get; set; }

		public required User Author { get; set; }

        public record RatingAlgorithmDto(int Id, string Formula, int AuthorId);

    }
}