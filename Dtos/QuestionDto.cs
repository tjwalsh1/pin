namespace Pinpoint_Quiz.Dtos
{
    public class QuestionDto
    {
        public int Id { get; set; }
        public string QuestionPrompt { get; set; }
        public string CorrectAnswer { get; set; }
        public List<string> WrongAnswers { get; set; } = new List<string>();
        public string Explanation { get; set; }
        public double Difficulty { get; set; }
        public string Subject { get; set; }
        public string YourAnswer { get; set; } // store user’s selected answer

        public List<string> ShuffledAnswers
        {
            get
            {
                var all = new List<string>(WrongAnswers);
                if (!string.IsNullOrEmpty(CorrectAnswer))
                {
                    all.Add(CorrectAnswer);
                }

                var rnd = new Random();
                for (int i = all.Count - 1; i > 0; i--)
                {
                    int swapIdx = rnd.Next(i + 1);
                    (all[i], all[swapIdx]) = (all[swapIdx], all[i]);
                }
                return all;
            }
        }
    }
}
