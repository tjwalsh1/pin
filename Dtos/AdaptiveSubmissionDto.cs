namespace Pinpoint_Quiz.Dtos
{
    public class AdaptiveSubmissionDto
    {
        public int StudentId { get; set; }
        public int QuestionNumber { get; set; }
        public string SelectedMathAnswer { get; set; }
        public string CorrectMathAnswer { get; set; }
        public string SelectedEbrwAnswer { get; set; }
        public string CorrectEbrwAnswer { get; set; }
    }
}
