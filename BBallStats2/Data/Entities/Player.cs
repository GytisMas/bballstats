namespace BBallStats.Data.Entities
{
	public enum PlayerRole
	{
		PointGuard = 0,
		ShootingGuard = 1,
		SmallForward = 2,
		PowerForward = 3,
		Center = 4
	}

	public class Player
	{
		public int Id { get; set; }
		public required string Name { get; set; }
		public required PlayerRole Role { get; set; }
		public required Team CurrentTeam { get; set; }

	}
}