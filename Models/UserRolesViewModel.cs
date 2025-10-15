namespace NewsWebsite.Models.ViewModels
{
    public class UserRolesViewModel
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
    }
}
