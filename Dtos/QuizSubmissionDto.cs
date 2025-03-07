namespace Pinpoint_Quiz.Dtos
{
    public class QuizSubmissionDto
    {
        public List<AnswerDto> EbrwAnswers { get; set; } = new List<AnswerDto>();
        public int QuizId { get; set; }
        public List<AnswerDto> MathAnswers { get; set; } = new List<AnswerDto>();
        public List<AnswerDto> QuestionResponses { get; set; }
    }
}
