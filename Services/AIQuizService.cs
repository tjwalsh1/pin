using MySqlConnector;
using Pinpoint_Quiz.Dtos;

namespace Pinpoint_Quiz.Services;

public class AIQuizService
{
    private readonly MySqlDatabase _db;

    public AIQuizService(MySqlDatabase db) => _db = db;

    public async Task SaveAIQuestions(List<QuestionDto> questions)
    {
        using var conn = _db.GetConnection();
        conn.Open();
        foreach (var q in questions)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Questions (Subject, Difficulty, Question_Prompt, Correct_Answer, Wrong_Answer1, Wrong_Answer2, Wrong_Answer3, Explanation)
                VALUES (@subject, @difficulty, @prompt, @correct, @wrong1, @wrong2, @wrong3, @explanation);
            ";
            cmd.Parameters.AddWithValue("@subject", q.Subject);
            cmd.Parameters.AddWithValue("@difficulty", q.Difficulty);
            cmd.Parameters.AddWithValue("@prompt", q.QuestionPrompt);
            cmd.Parameters.AddWithValue("@correct", q.CorrectAnswer);
            cmd.Parameters.AddWithValue("@wrong1", q.WrongAnswers[0]);
            cmd.Parameters.AddWithValue("@wrong2", q.WrongAnswers[1]);
            cmd.Parameters.AddWithValue("@wrong3", q.WrongAnswers[2]);
            cmd.Parameters.AddWithValue("@explanation", q.Explanation);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
