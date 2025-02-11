namespace Pinpoint_Quiz.Dtos
{
    public class QuizHistoryDto
    {
        public int Id { get; set; }
        public DateTime QuizDate { get; set; }
        public double MathProficiency { get; set; }
        public double EbrwProficiency { get; set; }
        public double OverallProficiency { get; set; }
        public int MathCorrect { get; set; }
        public int EbrwCorrect { get; set; }
        public int MathTotal { get; set; }
        public int EbrwTotal { get; set; }
        public int QuizId { get; set; }
        public double ActualOverallProficiency { get; set; }
        public double ActualMathProficiency { get; set; }
        public double ActualEbrwProficiency { get; set; }
        public DateTime TimeStarted { get; set; }
        public DateTime TimeEnded { get; set; }
        public double TimeElapsed { get; set; }
    }
}
