namespace Pinpoint_Quiz.Dtos
{
    public class NonAdaptiveSubmissionDto
    {
        public int StudentId { get; set; }
        public string Subject { get; set; }

        public Dictionary<int, string> AnswersMath { get; set; }
        public Dictionary<int, string> AnswersEbrw { get; set; }
    }
}
