namespace BBallStats.Data.Entities
{
	public enum UserType
	{
		Administrator = 0,
        Moderator = 1,
		Curator = 2,
		Regular = 3
	}

	public class User
    {
        public int Id { get; set; }
        public required string Username { get; set; }
		public required string Password { get; set; }
		public required string Email { get; set; }
		public required UserType Type { get; set; }
        public record UserDto(int Id, string Username, string Password, string Email, int Type);
    }
}