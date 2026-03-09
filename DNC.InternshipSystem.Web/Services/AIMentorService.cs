using System.Text;
using System.Text.Json;

namespace DNC.InternshipSystem.Web.Services
{
    public class AIMentorService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly ILogger<AIMentorService> _logger;
        private static readonly Dictionary<string, DateTime> _requestCache = new();
        private static readonly object _cacheLock = new();
        private const int MIN_REQUEST_INTERVAL_MS = 3000; // 3 giây giữa các request
        private const int MAX_RETRIES = 2;
        private const int INITIAL_RETRY_DELAY_MS = 2000;

        public AIMentorService(HttpClient http, IConfiguration config, ILogger<AIMentorService> logger)
        {
            _http = http;
            _config = config;
            _logger = logger;

            // Set timeout for API calls
            _http.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Rate limiting - ensure minimum interval between API requests
        /// </summary>
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
                        return false; // Need to wait
                    }
                }
                
                _requestCache["last_ai_request"] = DateTime.UtcNow;
                return true; // Can proceed
            }
        }

        public async Task<string> AnalyzeLogbook(string content)
        {
            try
            {
                // Wait for rate limit
                while (!await WaitForRateLimit())
                {
                    await Task.Delay(500);
                }

                var apiKey = _config["OpenAI:ApiKey"];

                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("OpenAI API key not configured, using fallback analysis");
                    return AnalyzeLogbookLocally(content);
                }

                // Retry logic with exponential backoff
                int retryCount = 0;
                int delayMs = INITIAL_RETRY_DELAY_MS;

                while (retryCount < MAX_RETRIES)
                {
                    try
                    {
                        return await CallOpenAIAPI(content, apiKey);
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
                        delayMs *= 2; // Exponential backoff
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

        /// <summary>
        /// Local analysis fallback when OpenAI is unavailable
        /// </summary>
        private string AnalyzeLogbookLocally(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return """
AI Mentor (Phân tích nội bộ):

**Tóm tắt nội dung:** Logbook trống hoặc không có nội dung.

**Chất lượng công việc:** 0/100

**Cảnh báo:** ⚠️ CẢNH BÁO - Sinh viên chưa nộp nội dung logbook.

**Gợi ý cải thiện:**
- Nhắc sinh viên nộp lại logbook với nội dung chi tiết
- Ghi rõ công việc đã thực hiện trong tuần
""";
            }

            var lines = content.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            var wordCount = content.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            var charCount = content.Length;

            // Assess quality based on content
            int score = 50; // Base score
            var warnings = new List<string>();
            var suggestions = new List<string>();

            // Check length
            if (charCount < 100)
            {
                score -= 30;
                warnings.Add("Nội dung quá ngắn");
                suggestions.Add("Cần bổ sung thêm chi tiết về công việc đã thực hiện");
            }
            else if (charCount < 300)
            {
                score -= 15;
                warnings.Add("Nội dung còn hơi ngắn");
                suggestions.Add("Nên mô tả chi tiết hơn về từng nhiệm vụ");
            }
            else if (charCount > 2000)
            {
                score -= 5;
                suggestions.Add("Có thể tóm tắt lại cho ngắn gọn hơn");
            }

            // Check for detail
            if (content.Contains("code") || content.Contains("debug") || content.Contains("fix"))
            {
                score += 15;
            }
            if (content.Contains("test") || content.Contains("deploy"))
            {
                score += 10;
            }
            if (content.Contains("error") || content.Contains("issue") || content.Contains("problem"))
            {
                score += 5;
            }

            // Grammar/structure check
            if (lines.Count >= 5)
            {
                score += 10;
                suggestions.Add("Cấu trúc logbook tốt");
            }

            score = Math.Min(100, Math.Max(0, score));

            var warningText = warnings.Any() ? string.Join("\n- ", warnings) : "Không có cảnh báo";
            var suggestionText = suggestions.Any() ? string.Join("\n- ", suggestions) : "Logbook khá tốt, hãy tiếp tục cố gắng";

            return $"""
AI Mentor (Phân tích nội bộ - Chế độ Fallback):

**Tóm tắt nội dung:** 
{(lines.Count > 0 ? lines[0] : content.Substring(0, Math.Min(100, content.Length)))}
(Tổng {wordCount} từ, {charCount} ký tự)

**Chất lượng công việc:** {score}/100

**Cảnh báo:**
- {warningText}

**Gợi ý cải thiện:**
- {suggestionText}

*Ghi chú: Đang sử dụng phân tích nội địa. Để có phân tích chi tiết hơn từ AI ChatGPT, vui lòng thử lại sau.*
""";
        }

        private async Task<string> CallOpenAIAPI(string content, string apiKey)
        {
            var request = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new {
                        role = "user",
                        content = $"""
Bạn là mentor thực tập IT chuyên nghiệp.

Hãy phân tích nhật ký logbook sau và trả lời ngắn gọn (dưới 250 từ):

1. **Tóm tắt nội dung** (1-2 câu)
2. **Chất lượng công việc** (0-100 điểm)
3. **Cảnh báo** (nếu có vấn đề)
4. **Gợi ý cải thiện** (1-2 điểm)

LOGBOOK:
{content}

PHẢN HỒI:
"""
                    }
                },
                max_tokens = 300,
                temperature = 0.3
            };

            var json = JsonSerializer.Serialize(request);

            var message = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.openai.com/v1/chat/completions"
            );

            message.Headers.Add("Authorization", $"Bearer {apiKey}");

            message.Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            _logger.LogInformation("Calling OpenAI API for logbook analysis");

            var response = await _http.SendAsync(message);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                throw new HttpRequestException("429-TooManyRequests");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"OpenAI API error: {response.StatusCode} - {errorContent}");
                
                var error = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized => "Unauthorized-InvalidKey",
                    System.Net.HttpStatusCode.BadRequest => "BadRequest",
                    System.Net.HttpStatusCode.TooManyRequests => "429-TooManyRequests",
                    _ => $"Error-{response.StatusCode}"
                };
                
                throw new HttpRequestException(error);
            }

            var responseJson = await response.Content.ReadAsStringAsync();

            // Parse response to extract content
            using var doc = JsonDocument.Parse(responseJson);
            var aiContent = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            _logger.LogInformation("Successfully analyzed logbook with OpenAI");

            return aiContent ?? "AI Mentor: Không thể phân tích nội dung.";
        }
    }
}