using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Pinpoint_Quiz.Dtos;

namespace Pinpoint_Quiz.Services
{
    public class AIQuizService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AIQuizService> _logger;
        private readonly MySqlDatabase _db;
        private readonly string _openAiApiKey;
        private readonly QuizService _quizService;

        private const string OpenAiApiUrl = "https://api.openai.com/v1/chat/completions";

        public AIQuizService(
            HttpClient httpClient,
            ILogger<AIQuizService> logger,
            IConfiguration configuration,
            MySqlDatabase db,
            QuizService quizService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _db = db;
            _quizService = quizService;
            _openAiApiKey = configuration["OpenAI:ApiKey"];
        }

        /// <summary>
        /// Generates questions based on the user's previously incorrect questions.
        /// If no incorrect questions exist, falls back to a generic subject/difficulty prompt.
        /// </summary>
        /// <param name="userId">User ID to use for retrieving past quiz data.</param>
        /// <param name="subject">Either "Math" or "EBRW".</param>
        /// <param name="difficulty">A number from 1 to 10 representing difficulty.</param>
        /// <param name="numQuestions">Number of questions to generate (default 10).</param>
        public async Task<List<QuestionDto>> GenerateQuestionsAsync(int userId, string subject, int difficulty, int numQuestions = 10)
        {
            // Get the user's recent incorrect questions.
            var incorrect = await GetIncorrectQuestionsAsync(userId);
            List<QuestionDto> generatedQuestions;
            if (incorrect.Any())
            {
                // Build a prompt that incorporates previous incorrect questions.
                string gradeLevel = MapDifficultyToGrade(difficulty);
                string prompt = BuildPromptBasedOnPreviousIncorrect(subject, gradeLevel, incorrect, numQuestions);
                generatedQuestions = await CallOpenAiAndInsertQuestionsAsync(subject, difficulty, prompt, numQuestions);
            }
            else
            {
                // Fallback: generate generic questions for the subject/difficulty.
                generatedQuestions = await GenerateSubjectDifficultyQuizAsync(subject, difficulty, numQuestions);
            }

            return generatedQuestions;
        }

        /// <summary>
        /// Maps a difficulty (1-10) to a grade level string.
        /// </summary>
        private string MapDifficultyToGrade(int difficulty)
        {
            return difficulty switch
            {
                1 => "5th grade",
                2 => "6th grade",
                3 => "7th grade",
                4 => "8th grade",
                5 => "9th grade (freshman)",
                6 => "10th grade (sophomore)",
                7 => "11th grade (junior)",
                8 => "12th grade (senior)",
                9 => "college freshman",
                10 => "college sophomore",
                _ => "unspecified"
            };
        }

        /// <summary>
        /// Builds a prompt that references the user's missed questions.
        /// </summary>
        private string BuildPromptBasedOnPreviousIncorrect(string subject, string gradeLevel, List<QuestionResultDto> incorrect, int numQuestions)
        {
            string intro = subject == "Math"
                ? $@"You are creating standardized test-style Math questions for {gradeLevel}.
IMPORTANT: Always output each question in EXACTLY the following format:

Question Prompt: <Text>
Correct Answer: <Text>
Wrong Answer 1: <Text>
Wrong Answer 2: <Text>
Wrong Answer 3: <Text>
Explanation: <Text>

Provide exactly {numQuestions} new questions.
Below are the user's previously missed questions. Generate new questions on similar topics and difficulty."
                : $@"You are creating standardized test-style English (EBRW) questions for {gradeLevel} students.
IMPORTANT: Always output each question in EXACTLY the following format:

Question: <Text>
Correct Answer: <Text>
Wrong Answer 1: <Text>
Wrong Answer 2: <Text>
Wrong Answer 3: <Text>
Explanation: <Text>

Provide exactly {numQuestions} new questions.
Below are the user's previously missed questions. Create new questions addressing similar topics.";

            string details = "";
            foreach (var q in incorrect.Take(numQuestions))
            {
                details += $"\nPreviously Missed Prompt: {q.QuestionPrompt}\nSubject: {q.Subject}\nDifficulty: {q.Difficulty}\n";
            }

            return intro + details;
        }

        /// <summary>
        /// Retrieves the user's incorrect questions from the last 10 quizzes.
        /// </summary>
        private async Task<List<QuestionResultDto>> GetIncorrectQuestionsAsync(int userId, int quizCount = 10)
        {
            var incorrect = new List<QuestionResultDto>();
            using var conn = _db.GetConnection();
            conn.Open();

            string sql = @"SELECT Questions FROM QuizResults 
                           WHERE UserId = @UserId 
                           ORDER BY QuizDate DESC 
                           LIMIT @Limit;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Limit", quizCount);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var questionsJson = reader.IsDBNull(0) ? "[]" : reader.GetString(0);
                var parsed = System.Text.Json.JsonSerializer.Deserialize<List<QuestionResultDto>>(questionsJson);
                if (parsed != null)
                {
                    incorrect.AddRange(parsed.Where(q => !q.IsCorrect));
                }
            }
            return incorrect;
        }
        public async Task<List<QuestionDto>> GenerateSplitAiQuestionsAsync(int userId)
        {
            // Retrieve incorrect questions from recent quizzes
            var incorrect = await GetIncorrectQuestionsAsync(userId);
            if (!incorrect.Any())
            {
                _logger.LogWarning("No incorrect questions found; using default difficulty values.");
            }

            // Filter by subject
            var mathIncorrect = incorrect.Where(q => q.Subject.Equals("Math", StringComparison.OrdinalIgnoreCase)).ToList();
            var ebrwIncorrect = incorrect.Where(q => q.Subject.Equals("EBRW", StringComparison.OrdinalIgnoreCase)).ToList();

            // Determine target difficulty dynamically (you can adjust the calculation as needed)
            int mathDifficulty = mathIncorrect.Any() ? (int)Math.Round(mathIncorrect.Average(q => q.Difficulty)) : 5;
            int ebrwDifficulty = ebrwIncorrect.Any() ? (int)Math.Round(ebrwIncorrect.Average(q => q.Difficulty)) : 5;

            // Build prompts for each subject
            string mathGradeLevel = MapDifficultyToGrade(mathDifficulty);
            string ebrwGradeLevel = MapDifficultyToGrade(ebrwDifficulty);

            string mathPrompt = BuildMathPrompt(mathGradeLevel, 5);  // generate 5 math questions
            string ebrwPrompt = BuildEbrwPrompt(ebrwGradeLevel, 5);    // generate 5 ebrw questions

            // Generate Math questions
            var mathQuestions = await CallOpenAiAndInsertQuestionsAsync("Math", mathDifficulty, mathPrompt, 5);
            // Generate EBRW questions
            var ebrwQuestions = await CallOpenAiAndInsertQuestionsAsync("EBRW", ebrwDifficulty, ebrwPrompt, 5);

            // Combine and return the list
            var allQuestions = new List<QuestionDto>();
            if (mathQuestions != null) allQuestions.AddRange(mathQuestions);
            if (ebrwQuestions != null) allQuestions.AddRange(ebrwQuestions);
            return allQuestions;
        }

        /// <summary>
        /// Calls the OpenAI API with the given prompt, parses the output, and inserts each question into the DB.
        /// </summary>
        private async Task<List<QuestionDto>> CallOpenAiAndInsertQuestionsAsync(string subject, int difficulty, string prompt, int numQuestions)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openAiApiKey}");

            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                    new { role = "system", content = "You are a professional test creation expert." },
                    new { role = "user", content = prompt }
                },
                max_tokens = 3000,
                temperature = 0.7
            };

            var response = await _httpClient.PostAsJsonAsync(OpenAiApiUrl, requestBody);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"OpenAI API call failed: {response.StatusCode}");
                return new List<QuestionDto>();
            }

            var apiResult = await response.Content.ReadFromJsonAsync<OpenAiApiResponse>();
            string rawQuestions = apiResult?.choices?[0]?.message?.content ?? "";

            var questions = ParseQuestions(rawQuestions, subject, difficulty);
            if (!questions.Any())
            {
                _logger.LogWarning("No valid questions parsed for subject {Subject} and difficulty {Difficulty}.", subject, difficulty);
                return questions;
            }

            // Insert each question into the DB and capture the new Id.
            foreach (var q in questions)
            {
                int newId = InsertQuestionIntoDb(q);
                if (newId > 0)
                {
                    q.Id = newId;
                }
            }
            return questions;
        }

        /// <summary>
        /// Fallback method: generates generic questions for the subject/difficulty.
        /// </summary>
        private async Task<List<QuestionDto>> GenerateSubjectDifficultyQuizAsync(string subject, int difficulty, int numQuestions)
        {
            string gradeLevel = MapDifficultyToGrade(difficulty);
            string prompt = subject == "EBRW"
                ? BuildEbrwPrompt(gradeLevel, numQuestions)
                : BuildMathPrompt(gradeLevel, numQuestions);

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openAiApiKey}");

            var requestBody = new
            {
                model = "gpt-4",
                messages = new[]
                {
                    new { role = "system", content = "You are a professional test creation expert." },
                    new { role = "user", content = prompt }
                },
                max_tokens = 3000,
                temperature = 0.7
            };

            var response = await _httpClient.PostAsJsonAsync(OpenAiApiUrl, requestBody);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"OpenAI API call failed: {response.StatusCode}");
                return new List<QuestionDto>();
            }

            var apiResult = await response.Content.ReadFromJsonAsync<OpenAiApiResponse>();
            string rawQuestions = apiResult?.choices?[0]?.message?.content ?? "";

            var questions = ParseQuestions(rawQuestions, subject, difficulty);
            foreach (var q in questions)
            {
                int newId = InsertQuestionIntoDb(q);
                if (newId > 0) q.Id = newId;
            }
            return questions;
        }

        /// <summary>
        /// Builds a prompt for EBRW questions.
        /// </summary>
        private string BuildEbrwPrompt(string gradeLevel, int numQuestions)
        {
            return $@"
You are creating standardized test-style English (EBRW) questions for {gradeLevel} students.
IMPORTANT: Always output each question in EXACTLY the following format, with no extra numbering or markdown:

Question: <Text>
Correct Answer: <Text>
Wrong Answer 1: <Text>
Wrong Answer 2: <Text>
Wrong Answer 3: <Text>
Explanation: <Text>

Provide exactly {numQuestions} questions.
Ensure questions are unique and reflect appropriate difficulty.
";
        }

        /// <summary>
        /// Builds a prompt for Math questions.
        /// </summary>
        private string BuildMathPrompt(string gradeLevel, int numQuestions)
        {
            return $@"
You are creating standardized test-style Math questions for {gradeLevel} students.
IMPORTANT: Always output each question in EXACTLY the following format, with no extra numbering or markdown:

Question Prompt: <Text>
Correct Answer: <Text>
Wrong Answer 1: <Text>
Wrong Answer 2: <Text>
Wrong Answer 3: <Text>
Explanation: <Text>

Provide exactly {numQuestions} questions.
Cover diverse math topics and ensure the difficulty matches {gradeLevel}.
";
        }

        /// <summary>
        /// Parses the raw AI output into a list of QuestionDto objects.
        /// This regex handles both "Question:" (EBRW) and "Question Prompt:" (Math) formats.
        /// </summary>
        private List<QuestionDto> ParseQuestions(string raw, string subject, int difficulty)
        {
            var list = new List<QuestionDto>();
            var pattern = new Regex(
                @"(?:Question Prompt|Question):\s*(.*?)\n" +
                @"Correct Answer:\s*(.*?)\n" +
                @"Wrong Answer 1:\s*(.*?)\n" +
                @"Wrong Answer 2:\s*(.*?)\n" +
                @"Wrong Answer 3:\s*(.*?)\n" +
                @"Explanation:\s*(.*?)(?=\n(?:Question Prompt|Question):|$)",
                RegexOptions.Singleline | RegexOptions.IgnoreCase
            );

            var matches = pattern.Matches(raw);
            foreach (Match m in matches)
            {
                if (m.Groups.Count == 7)
                {
                    list.Add(new QuestionDto
                    {
                        Subject = subject,
                        Difficulty = difficulty,
                        QuestionPrompt = m.Groups[1].Value.Trim(),
                        CorrectAnswer = m.Groups[2].Value.Trim(),
                        WrongAnswers = new List<string>
                        {
                            m.Groups[3].Value.Trim(),
                            m.Groups[4].Value.Trim(),
                            m.Groups[5].Value.Trim()
                        },
                        Explanation = m.Groups[6].Value.Trim()
                    });
                }
                else
                {
                    _logger.LogWarning("A question block failed parsing for subject {Subject}.", subject);
                }
            }
            return list;
        }

        /// <summary>
        /// Inserts a question into the Questions table and returns the new question's Id.
        /// </summary>
        private int InsertQuestionIntoDb(QuestionDto question)
        {
            int newId = 0;
            try
            {
                using var conn = _db.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
INSERT INTO Questions 
    (Subject, Difficulty, Question_Prompt, Correct_Answer, 
     Wrong_Answer1, Wrong_Answer2, Wrong_Answer3, Explanation)
VALUES
    (@Subj, @Diff, @Prompt, @Correct,
     @W1, @W2, @W3, @Expl);";

                cmd.Parameters.AddWithValue("@Subj", question.Subject);
                cmd.Parameters.AddWithValue("@Diff", question.Difficulty);
                cmd.Parameters.AddWithValue("@Prompt", question.QuestionPrompt);
                cmd.Parameters.AddWithValue("@Correct", question.CorrectAnswer);
                cmd.Parameters.AddWithValue("@W1", question.WrongAnswers.ElementAtOrDefault(0) ?? "");
                cmd.Parameters.AddWithValue("@W2", question.WrongAnswers.ElementAtOrDefault(1) ?? "");
                cmd.Parameters.AddWithValue("@W3", question.WrongAnswers.ElementAtOrDefault(2) ?? "");
                cmd.Parameters.AddWithValue("@Expl", question.Explanation ?? "");

                cmd.ExecuteNonQuery();
                newId = Convert.ToInt32(cmd.LastInsertedId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to insert AI-generated question into the database.");
            }
            return newId;
        }

        // Helper classes for OpenAI API response
        private class OpenAiApiResponse
        {
            public List<OpenAiChoice> choices { get; set; }
        }

        private class OpenAiChoice
        {
            public OpenAiMessage message { get; set; }
        }

        private class OpenAiMessage
        {
            public string content { get; set; }
        }
    }
}
