using System.ComponentModel.DataAnnotations;

namespace Asset_Management_System.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        public string? SchoolID { get; set; }

        [Required]
        public string? LastName { get; set; }

        [Required]
        public string? FirstName { get; set; }

        [Required]
        public string? Username { get; set; }

        [Required]
        public string? Password { get; set; }

        [Required]
        public string? Role { get; set; }

        [Required, EmailAddress]
        public string? Email { get; set; }
    }
}
