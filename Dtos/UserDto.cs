namespace Pinpoint_Quiz.Dtos
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Grade { get; set; }
        public int? ClassId { get; set; }
        public int? SchoolId { get; set; }
        public string UserRole { get; set; }
        public double ProficiencyMath { get; set; }
        public double ProficiencyEbrw { get; set; }
        public double OverallProficiency { get; set; }
    }
}
