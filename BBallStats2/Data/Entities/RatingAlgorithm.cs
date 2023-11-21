using BBallStats2.Auth.Model;
using System.ComponentModel.DataAnnotations;

namespace BBallStats.Data.Entities
{
	public class RatingAlgorithm
	{
		public int Id { get; set; }
		public required string Formula { get; set; }
		public required bool Promoted { get; set; }

        [Required]
        public required string UserId { get; set; }
        public ForumRestUser User { get; set; }

        public record RatingAlgorithmDto(int Id, string Formula, bool Promoted, string AuthorId);

    }
}