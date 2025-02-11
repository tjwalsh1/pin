using System.ComponentModel.DataAnnotations;

namespace Pinpoint_Quiz.Dtos
{
    public class RegisterDto
    {
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public int Grade { get; set; }

        [Required]
        public int ClassId { get; set; }

        [Required]
        public int SchoolId { get; set; }

        [Required]
        public string UserRole { get; set; } = "Student";
    }
}
