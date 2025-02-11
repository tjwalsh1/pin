namespace Pinpoint_Quiz.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Grade { get; set; }
        public int? ClassId { get; set; }
        public int? SchoolId { get; set; }
        public double ProficiencyMath { get; set; }
        public double ProficiencyEbrw { get; set; }
        public double OverallProficiency { get; set; }
        public double AvgQuizTime { get; set; }
        public string UserRole { get; set; }
    }
}

namespace Pinpoint_Quiz.Models
{
    public class Performance
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime Week { get; set; }
        public double ProficiencyMath { get; set; }
        public double ProficiencyEbrw { get; set; }
        public double OverallProficiency { get; set; }
    }
}

public class Quiz
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string Questions { get; set; } // JSON string of questions
    public double? Score { get; set; }

    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// History of quizzes taken
public class QuizHistoryRecord
{
    public int Id { get; set; }           // PK
    public int UserId { get; set; }
    public DateTime QuizDate { get; set; }
    public double MathProficiency { get; set; }
    public double EbrwProficiency { get; set; }
    public double OverallProficiency { get; set; }
    public int MathCorrect { get; set; }
    public int EbrwCorrect { get; set; }
    public int MathTotal { get; set; }
    public int EbrwTotal { get; set; }
    public double ActualMathProficiency { get; set; }
    public double ActualEbrwProficiency { get; set; }
    public double ActualOverallProficiency { get; set; }
    public double TimeElapsed { get; set; }
}
