using Pinpoint_Quiz.Dtos;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pinpoint_Quiz.Services
{
    public class AIQuizService
    {
        private readonly HttpClient _httpClient;
        private readonly string _openAiApiKey;

        public AIQuizService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;

            // 1) If you want to store your key in appsettings.json:
            // _openAiApiKey = config["OpenAI:ApiKey"]; 
            // 2) But you specifically said "use my API key" - so let's do:
            _openAiApiKey = config["OpenAI:ApiKey"];
        }

        public async Task<List<QuestionDto>> GenerateQuestionsAsync(List<string> incorrectPrompts)
        {
            // Build a user message with the incorrect prompts
            var sb = new StringBuilder();
            sb.AppendLine("The student got the following questions wrong:");
            for (int i = 0; i < incorrectPrompts.Count; i++)
            {
                sb.AppendLine($"{i + 1}. {incorrectPrompts[i]}");
            }
            sb.AppendLine();
            sb.AppendLine("Generate 10 new multiple-choice questions that are similar in content and difficulty.");
            sb.AppendLine("For each question, please format them as follows:");
            sb.AppendLine("Question: <Text including passage if needed and referenced>");
            sb.AppendLine("Choices:");
            sb.AppendLine("  A) ...");
            sb.AppendLine("  B) ...");
            sb.AppendLine("  C) ...");
            sb.AppendLine("  D) ...");
            sb.AppendLine("Correct Answer: X");
            sb.AppendLine("Explanation: <detailed explanation>");
            sb.AppendLine();
            sb.AppendLine("No extraneous text, just the questions in the specified format. Thank you.");

            // We’ll send a chat-style request to GPT-4
            var chatRequest = new ChatRequest
            {
                Model = "gpt-4o",  // GPT-4 model
                Messages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        Role = "user",
                        Content = sb.ToString()
                    }
                },
                MaxTokens = 2000,
                Temperature = 0.7
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(chatRequest),
                Encoding.UTF8,
                "application/json"
            );

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _openAiApiKey);

            // Call the new v1/chat/completions endpoint
            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", jsonContent);
            if (!response.IsSuccessStatusCode)
            {
                // handle error
                return new List<QuestionDto>();
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var openAiResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(responseString);

            var generatedText = openAiResponse?.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(generatedText))
                return new List<QuestionDto>();

            // Parse the text into structured questions
            var parsedQuestions = ParseQuestions(generatedText);
            return parsedQuestions;
        }

        private List<QuestionDto> ParseQuestions(string text)
        {
            // TODO: parse the raw chat response into your QuestionDto objects
            // This depends heavily on how GPT-4 structures the text. You might
            // do some regex matching or a custom parser. Below is a placeholder.

            return new List<QuestionDto>();
        }
    }

    // Minimal classes for Chat-based calls

    public class ChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("messages")]
        public List<ChatMessage> Messages { get; set; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }
    }

    public class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    public class ChatCompletionResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("choices")]
        public List<ChatCompletionChoice> Choices { get; set; }
    }

    public class ChatCompletionChoice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("message")]
        public ChatMessage Message { get; set; }
    }
}
