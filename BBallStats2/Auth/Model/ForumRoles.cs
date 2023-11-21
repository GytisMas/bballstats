namespace BBallStats2.Auth.Model
{
    public static class ForumRoles
    {
        public const string Admin = nameof(Admin);
        public const string Moderator = nameof(Moderator);
        public const string Curator = nameof(Curator);
        public const string Regular = nameof(Regular);

        public static IReadOnlyCollection<string> All = new[] { Admin, Moderator, Curator, Regular };
    }
}
