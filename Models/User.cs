using System.ComponentModel.DataAnnotations;

namespace RiceMillProject.Models
{
    public class User
    {
        public int UserId { get; set; }
        
        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;

        public int PersonId { get; set; }
        public string PersonName { get; set; } = string.Empty;
        public int o10_postid { get; set; }
        public int sa10_usertypeid { get; set; }
        public string Role { get; set; } = string.Empty;
        public string LayoutName { get; set; } = "_Layout";
    }
}
