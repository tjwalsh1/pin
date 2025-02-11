namespace Pinpoint_Quiz.Dtos
{
    public class QuestionResultDto
    {
        public string QuestionPrompt { get; set; }
        public string YourAnswer { get; set; }
        public string CorrectAnswer { get; set; }
        public string Explanation { get; set; }
        public bool IsCorrect { get; set; }
        public string Subject { get; set; }
        public double Difficulty { get; set; }
        public bool RetakeMode { get; set; }
    }
}
