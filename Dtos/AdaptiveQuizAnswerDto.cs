namespace Pinpoint_Quiz.Dtos
{
    public class AdaptiveQuizAnswerDto
    {
        public int StudentId { get; set; }
        public int QuestionNumber { get; set; }
        public string mathPrompt { get; set; }
        public string correctMathAnswer { get; set; }
        public string selectedMathAnswer { get; set; }

        public string ebrwPrompt { get; set; }
        public string correctEbrwAnswer { get; set; }
        public string selectedEbrwAnswer { get; set; }
    }
}
