namespace BBallStats.Data.Entities
{
    public class AlgorithmImpression
    {
        public int Id { get; set; }
        public required bool Positive { get; set; }
        public required User User { get; set; }
        public required RatingAlgorithm RatingAlgorithm { get; set; }

        public record AlgorithmImpressionDto(int Id, bool positive, int UserId, int algorithm);
    }
}