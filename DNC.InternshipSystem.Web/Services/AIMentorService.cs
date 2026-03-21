using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DNC.InternshipSystem.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DNC.InternshipSystem.Web.Services
{
    public class AIMentorService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly ILogger<AIMentorService> _logger;
        private static readonly Dictionary<string, DateTime> _requestCache = new();
        private static readonly object _cacheLock = new();
        private const int MIN_REQUEST_INTERVAL_MS = 3000;
        private const int MAX_RETRIES = 2;
        private const int INITIAL_RETRY_DELAY_MS = 2000;

        public AIMentorService(HttpClient http, IConfiguration config, ILogger<AIMentorService> logger)
        {
            _http = http;
            _config = config;
            _logger = logger;
            _http.Timeout = TimeSpan.FromSeconds(30);
        }

        private async Task<bool> WaitForRateLimit()
        {
            lock (_cacheLock)
            {
                var lastRequestKey = "last_ai_request";
                if (_requestCache.TryGetValue(lastRequestKey, out var lastRequest))
                {
                    var timeSinceLastRequest = DateTime.UtcNow - lastRequest;
                    if (timeSinceLastRequest.TotalMilliseconds < MIN_REQUEST_INTERVAL_MS)
                    {
                        var waitTime = MIN_REQUEST_INTERVAL_MS - (int)timeSinceLastRequest.TotalMilliseconds;
                        _logger.LogInformation($"Rate limiting: waiting {waitTime}ms before next request");
                        return false;
                    }
                }

                _requestCache["last_ai_request"] = DateTime.UtcNow;
                return true;
            }
        }

        // Trả về trực tiếp đối tượng Logbook
        public async Task<Logbook> AnalyzeLogbook(string content)
        {
            try
            {
                while (!await WaitForRateLimit())
                {
                    await Task.Delay(500);
                }

                var apiKey = _config["Gemini:ApiKey"];

                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("Gemini API key not configured, using fallback analysis");
                    return AnalyzeLogbookLocally(content);
                }

                int retryCount = 0;
                int delayMs = INITIAL_RETRY_DELAY_MS;

                while (retryCount < MAX_RETRIES)
                {
                    try
                    {
                        return await CallGeminiAPI(content, apiKey);
                    }
                    catch (HttpRequestException ex) when (ex.Message.Contains("429") || ex.Message.Contains("TooManyRequests"))
                    {
                        retryCount++;
                        if (retryCount >= MAX_RETRIES)
                        {
                            _logger.LogWarning("Max retries exceeded, switching to fallback analysis");
                            return AnalyzeLogbookLocally(content);
                        }

                        _logger.LogWarning($"Rate limited (429). Retry {retryCount}/{MAX_RETRIES} after {delayMs}ms");
                        await Task.Delay(delayMs);
                        delayMs *= 2;
                    }
                    catch (Exception ex) when (ex.Message.Contains("quota") || ex.Message.Contains("limit"))
                    {
                        _logger.LogWarning("API quota/limit exceeded, using fallback analysis");
                        return AnalyzeLogbookLocally(content);
                    }
                }

                return AnalyzeLogbookLocally(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AnalyzeLogbook, using fallback");
                return AnalyzeLogbookLocally(content);
            }
        }

        private async Task<Logbook> CallGeminiAPI(string content, string apiKey)
        {
            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new[] {
                        new { text = "Bạn là AI Mentor. Hãy phân tích logbook và trả về DUY NHẤT JSON với CÁC TÊN KEY CHÍNH XÁC SAU: \"AISummary\" (chuỗi, tóm tắt 2 câu), \"AiScore\" (số nguyên 0-100), \"AIWarning\" (boolean, true nếu làm sơ sài), \"AIWarningDetails\" (chuỗi, lý do cảnh báo, rỗng nếu không có), \"AiSuggestions\" (chuỗi, 2 gạch đầu dòng gợi ý)." }
                    }
                },
                contents = new[]
                {
                    new {
                        role = "user",
                        parts = new[] { new { text = $"Nội dung logbook:\n{content}" } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.2,
                    response_mime_type = "application/json"
                }
            };

            var jsonRequest = JsonSerializer.Serialize(requestBody);
            var message = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json")
            };

            _logger.LogInformation("Đang gọi Gemini 1.5 Pro API...");
            var response = await _http.SendAsync(message);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                throw new HttpRequestException("429-TooManyRequests");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Gemini API error: {response.StatusCode} - {errorContent}");
                throw new HttpRequestException($"Error-{response.StatusCode}");
            }

            var responseString = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseString);
            var aiText = doc.RootElement
    .GetProperty("candidates")[0]
    .GetProperty("content")
    .GetProperty("parts")[0]
    .GetProperty("text")
    .GetString();

            if (string.IsNullOrEmpty(aiText))
            {
                return AnalyzeLogbookLocally(content);
            }

            // THÊM 2 DÒNG NÀY ĐỂ XÓA MARKDOWN (NẾU CÓ)
            aiText = aiText.Replace("```json", "").Replace("```", "").Trim();

            // Deserialize trực tiếp vào class Logbook
            var result = JsonSerializer.Deserialize<Logbook>(aiText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result ?? AnalyzeLogbookLocally(content);
        }

        private Logbook AnalyzeLogbookLocally(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return new Logbook
                {
                    AISummary = "Logbook trống hoặc không có nội dung.",
                    AiScore = 0,
                    AIWarning = true,
                    AIWarningDetails = "Sinh viên chưa nộp nội dung logbook.",
                    AiSuggestions = "- Nhắc sinh viên nộp lại logbook với nội dung chi tiết.\n- Ghi rõ công việc đã thực hiện trong tuần."
                };
            }

            var lines = content.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            var charCount = content.Length;

            int score = 50;
            var warnings = new List<string>();
            var suggestions = new List<string>();

            if (charCount < 100)
            {
                score -= 30;
                warnings.Add("Nội dung quá ngắn.");
                suggestions.Add("Cần bổ sung thêm chi tiết về công việc đã thực hiện.");
            }
            else if (charCount < 300)
            {
                score -= 15;
                warnings.Add("Nội dung còn hơi ngắn.");
                suggestions.Add("Nên mô tả chi tiết hơn về từng nhiệm vụ.");
            }

            if (content.Contains("code") || content.Contains("debug") || content.Contains("fix")) score += 15;
            if (content.Contains("test") || content.Contains("deploy")) score += 10;
            if (content.Contains("error") || content.Contains("issue") || content.Contains("problem")) score += 5;
            if (lines.Count >= 5)
            {
                score += 10;
                suggestions.Add("Cấu trúc logbook tốt.");
            }

            score = Math.Min(100, Math.Max(0, score));
            bool hasWarn = warnings.Any();

            return new Logbook
            {
                AISummary = lines.Count > 0 ? lines[0] : content.Substring(0, Math.Min(100, content.Length)),
                AiScore = score,
                AIWarning = hasWarn,
                AIWarningDetails = hasWarn ? string.Join("\n", warnings) : string.Empty,
                AiSuggestions = suggestions.Any() ? string.Join("\n- ", suggestions) : "Logbook khá tốt, hãy tiếp tục cố gắng."
            };
        }
    }
}