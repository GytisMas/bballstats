using Microsoft.AspNetCore.Identity;

namespace BBallStats2.Auth.Model
{
    public class ForumRestUser : IdentityUser
    {
        public bool ForceRelogin { get; set; }   
    }
}
