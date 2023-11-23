using BBallStats2.Auth.Model;
using System.ComponentModel.DataAnnotations;

namespace BBallStats.Data.Entities
{
    public class AlgorithmImpression
    {
        public int Id { get; set; }
        public required bool Positive { get; set; }
        public required RatingAlgorithm RatingAlgorithm { get; set; }

        [Required]
        public required string UserId { get; set; }
        public ForumRestUser User { get; set; }

        public record AlgorithmImpressionDto(int Id, bool positive, int algorithm, string userId);
    }
}