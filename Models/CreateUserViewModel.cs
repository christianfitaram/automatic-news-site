using System.ComponentModel.DataAnnotations;

namespace NewsWebsite.Models.ViewModels
{
    public class CreateUserViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string SelectedRole { get; set; } = string.Empty;

        public List<string> AvailableRoles { get; set; } = new();
    }
}
