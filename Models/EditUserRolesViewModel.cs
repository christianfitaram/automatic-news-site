namespace NewsWebsite.Models.ViewModels
{
    public class EditUserRolesViewModel
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public List<string> AvailableRoles { get; set; } = new();
        public IList<string> SelectedRoles { get; set; } = new List<string>();
    }
}
