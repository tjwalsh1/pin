using Pinpoint_Quiz.Dtos;
using System.Collections.Generic;

namespace Pinpoint_Quiz.Models
{
    public class AdaptiveQuizViewModel
    {
        public int StudentId { get; set; }
        public int QuestionNumber { get; set; }
        public int TotalQuestions { get; set; }
        public Dtos.QuestionDto EbrwQuestion { get; set; }
        public Dtos.QuestionDto MathQuestion { get; set; }
    }
}

namespace Pinpoint_Quiz.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}

namespace Pinpoint_Quiz.Models
{
    public class NonAdaptiveQuizViewModel
    {
        public int StudentId { get; set; }
        public string Subject { get; set; }
        public List<QuestionDto> MathQuestions { get; set; }
        public List<QuestionDto> EbrwQuestions { get; set; }
    }
}

namespace Pinpoint_Quiz.Models
{
    public class PerformanceChartViewModel
    {
        public List<string> LabelDates { get; set; } = new List<string>();
        public List<double> MathLevels { get; set; } = new List<double>();
        public List<double> EbrwLevels { get; set; } = new List<double>();
        public List<double> OverallLevels { get; set; } = new List<double>();
        public bool IsLoggedIn { get; set; }
        public List<double> TimeElapsed { get; set; } = new List<double>();

        //public List<double> TimeElapsed { get; set; } = new List<double>();
        public List<QuizHistoryDto> QuizHistory { get; set; } = new List<QuizHistoryDto>();
        // New properties for actual proficiency levels:
        public List<double> ActualMathLevels { get; set; } = new List<double>();
        public List<double> ActualEbrwLevels { get; set; } = new List<double>();
        public List<double> ActualOverallLevels { get; set; } = new List<double>();
    }
}


namespace Pinpoint_Quiz.Models
{
    public class ProfileViewModel
    {
        // User Details
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public int Grade { get; set; }
        public int ClassId { get; set; }
        public int SchoolId { get; set; }
        public double ProficiencyMath { get; set; }
        public double ProficiencyEbrw { get; set; }
        public double OverallProficiency { get; set; }

        // Performance Data for Graph
        public List<string> Dates { get; set; } = new List<string>();
        public List<double> MathProficiencies { get; set; } = new List<double>();
        public List<double> EbrwProficiencies { get; set; } = new List<double>();

        // Accolades
        public List<AccoladeDto> Accolades { get; set; }

        // Recent Quizzes
        public List<QuizHistoryRecord> RecentQuizzes { get; set; } = new List<QuizHistoryRecord>();
    }
}

namespace Pinpoint_Quiz.Models
{
    public class QuizzesIndexViewModel
    {
        public double StartingDifficulty { get; set; }
        public bool IsLoggedIn { get; set; }
        public List<QuizHistoryDto> QuizHistory { get; set; } = new List<QuizHistoryDto>();

    }
}


namespace Pinpoint_Quiz.Models
{
    public class QuestionRecord
    {
        public string Subject { get; set; } // "Math" or "EBRW"
        public QuestionDto Dto { get; set; }
        public bool? UserCorrect { get; set; }
    }
}

namespace Pinpoint_Quiz.Models
{
    // Consolidated single class
    public class SingleQuestionViewModel
    {
        public int StudentId { get; set; }
        public int QuestionNumber { get; set; }
        public int TotalQuestions { get; set; }
        public string Prompt { get; set; }
        public List<string> Answers { get; set; }
        public string CorrectAnswer { get; set; }
        public string Explanation { get; set; }
        public double Difficulty { get; set; }
        public string Subject { get; set; }
        public int QuestionId { get; set; }
    }
}


namespace Pinpoint_Quiz.Models
{
    public class QuizResults
    {
        public DateTime QuizDate { get; set; }
        public int StudentId { get; set; }
        public int MathCorrect { get; set; }
        public int MathTotal { get; set; }
        public int EbrwCorrect { get; set; }
        public int EbrwTotal { get; set; }

        // Computed properties (read-only)
        public int CorrectCount => MathCorrect + EbrwCorrect;
        public int TotalCount => MathTotal + EbrwTotal;

        public List<QuestionResultDto> QuestionResults { get; set; } = new List<QuestionResultDto>();
        public double FinalProficiencyMath { get; set; }
        public double FinalProficiencyEbrw { get; set; }
        public double FinalOverallProficiency { get; set; }
        public double ActualEbrwProficiency { get; set; }
        public double ActualMathProficiency { get; set; }
        public double ActualOverallProficiency { get; set; }
        public DateTime TimeStarted { get; set; }
        public DateTime TimeEnded { get; set; }
        public double TimeElapsed { get; set; }
    }
}

namespace Pinpoint_Quiz.Models
{
    public class QuizSession
    {
        public int UserId { get; set; }
        public bool IsAdaptive { get; set; }
        public int QuizId { get; set; }
        public int CurrentIndex { get; set; } = 0;
        public int TotalQuestions => 10; // for example: 10 total
        public double LocalMath { get; set; }
        public double LocalEbrw { get; set; }
        public int EbrwCount { get; set; }
        public int MathCount { get; set; }
        public List<QuestionRecord> Questions { get; set; } = new List<QuestionRecord>();
        public bool RetakeMode { get; set; } = false;
        public DateTime TimeStarted { get; set; }
        public DifficultyMode DifficultyMode { get; set; } = DifficultyMode.Normal;

    }
    namespace Pinpoint_Quiz.Models
    {
        public class Lesson
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string Subject { get; set; }  
            public int Level { get; set; }         
            public string Content { get; set; }     
            public string VideoUrl { get; set; }     
        }
    }

}
