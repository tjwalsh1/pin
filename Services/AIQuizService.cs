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
        private const string OpenAiApiUrl = "https://api.openai.com/v1/chat/completions";

        public AIQuizService(HttpClient httpClient, ILogger<AIQuizService> logger, IConfiguration configuration, MySqlDatabase db)
        {
            _httpClient = httpClient;
            _logger = logger;
            _db = db;
            _openAiApiKey = configuration["OpenAI:ApiKey"];
        }

        public async Task<List<QuestionDto>> GenerateQuestionsAsync(int userId)
        {
            // Get user's incorrect questions from last 10 quizzes
            var incorrectQuestions = await GetIncorrectQuestionsAsync(userId);
            if (!incorrectQuestions.Any())
            {
                _logger.LogWarning("User has no recent incorrect questions; falling back to default questions.");
                return new List<QuestionDto>();
            }

            // Prepare instructions for AI
            string aiPrompt = BuildAiPrompt(incorrectQuestions);

            // Call OpenAI API
            var apiRequest = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                    new { role = "system", content = "You are a professional test creation expert." },
                    new { role = "user", content = aiPrompt }
                },
                max_tokens = 3000,
                temperature = 0.7
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openAiApiKey}");

            var response = await _httpClient.PostAsJsonAsync(OpenAiApiUrl, apiRequest);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"OpenAI API call failed: {response.StatusCode}");
                return new List<QuestionDto>();
            }

            var apiResult = await response.Content.ReadFromJsonAsync<OpenAiApiResponse>();
            string rawQuestions = apiResult?.choices?[0]?.message?.content ?? "";

            return ParseQuestions(rawQuestions);
        }

        private async Task<List<QuestionResultDto>> GetIncorrectQuestionsAsync(int userId)
        {
            var questions = new List<QuestionResultDto>();
            using var conn = _db.GetConnection();
            // Remove the extra call to open the connection; it's already open.

            string sql = @"SELECT Questions FROM QuizResults WHERE UserId = @UserId ORDER BY QuizDate DESC LIMIT 10;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var questionsJson = reader.IsDBNull(0) ? "[]" : reader.GetString(0);
                var parsedQuestions = System.Text.Json.JsonSerializer.Deserialize<List<QuestionResultDto>>(questionsJson);
                questions.AddRange(parsedQuestions?.Where(q => !q.IsCorrect) ?? new List<QuestionResultDto>());
            }
            return questions;
        }


        private string BuildAiPrompt(List<QuestionResultDto> incorrectQuestions)
        {
            var prompt = @"
Generate exactly 10 standardized-test style questions based on the provided previously missed questions. 
For each provided question, generate a new question of the same topic and difficulty level.

IMPORTANT: Output each question in exactly this format (no numbering):

Question Prompt: <text>
Correct Answer: <text>
Wrong Answer 1: <text>
Wrong Answer 2: <text>
Wrong Answer 3: <text>
Explanation: <text>

Here are the previously missed questions:

";

            foreach (var q in incorrectQuestions.Take(10))
            {
                prompt += $@"
Original Question Topic: {q.Subject}
Original Question Difficulty: {q.Difficulty}
Original Question Prompt: {q.QuestionPrompt}

";
            }

            return prompt;
        }

        private List<QuestionDto> ParseQuestions(string rawQuestions)
        {
            var questions = new List<QuestionDto>();
            var regexPattern = new Regex(
                @"Question Prompt:\s*(.*?)\n" +
                @"Correct Answer:\s*(.*?)\n" +
                @"Wrong Answer 1:\s*(.*?)\n" +
                @"Wrong Answer 2:\s*(.*?)\n" +
                @"Wrong Answer 3:\s*(.*?)\n" +
                @"Explanation:\s*(.*?)(?=\nQuestion Prompt:|\z)",
                RegexOptions.Singleline | RegexOptions.IgnoreCase
            );

            var matches = regexPattern.Matches(rawQuestions);
            foreach (Match match in matches)
            {
                if (match.Groups.Count == 7)
                {
                    questions.Add(new QuestionDto
                    {
                        Subject = "AI-Generated",
                        Difficulty = 0, // AI-generated questions could inherit the original difficulty if needed
                        QuestionPrompt = match.Groups[1].Value.Trim(),
                        CorrectAnswer = match.Groups[2].Value.Trim(),
                        WrongAnswers = new List<string>
                        {
                            match.Groups[3].Value.Trim(),
                            match.Groups[4].Value.Trim(),
                            match.Groups[5].Value.Trim()
                        },
                        Explanation = match.Groups[6].Value.Trim()
                    });
                }
                else
                {
                    _logger.LogWarning("A question failed parsing and was skipped.");
                }
            }

            return questions;
        }

        // Helper classes to deserialize OpenAI API response
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
