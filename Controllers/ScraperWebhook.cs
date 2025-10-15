using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NewsWebsite.Services.Articles;
using System.Linq;

namespace NewsWebsite.Controllers
{
    [ApiController]
    [Route("api/scraper/[controller]")]
    public class WebhookController : ControllerBase
    {
        private const string WebhookSecret = "eyJAdminK3y-2025!zXt9fGHEMPLq4RsVm7DwuJXeb6u";

        private readonly IArticlesService _articlesService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WebhookController> _logger;

        public WebhookController(
            IArticlesService articlesService,
            IHttpClientFactory httpClientFactory,
            ILogger<WebhookController> logger)
        {
            _articlesService = articlesService ?? throw new ArgumentNullException(nameof(articlesService));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveWebhook(
            [FromHeader(Name = "X-Signature")] string? signature,
            CancellationToken cancellationToken)
        {
            if (!string.Equals(signature, WebhookSecret, StringComparison.Ordinal))
            {
                _logger.LogWarning("Invalid webhook signature received: {Signature}", signature);
                return Unauthorized("Firma inválida");
            }

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync(cancellationToken);

            _logger.LogInformation("Webhook payload received: {Payload}", body);

            ScrapedArticlePayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<ScrapedArticlePayload>(
                    body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Invalid JSON payload received from webhook");
                return BadRequest($"Error procesando JSON: {ex.Message}");
            }

            if (payload == null)
            {
                return BadRequest("Payload vacío o inválido.");
            }

            var sanitizedText = await SanitizeArticleTextAsync(payload.Text ?? string.Empty, CancellationToken.None);

            var articleRequest = BuildCreateArticleRequest(payload, sanitizedText);

            try
            {
                var article = await _articlesService.CreateArticleAsync(articleRequest, CancellationToken.None);
                return Ok(new { message = "Webhook recibido correctamente", articleId = article.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook payload");
                return StatusCode(500, "Error procesando la solicitud.");
            }
        }

        private CreateArticleRequest BuildCreateArticleRequest(ScrapedArticlePayload payload, string sanitizedText)
        {
            DateTime? publishedAt = null;
            if (!string.IsNullOrWhiteSpace(payload.ScrapedAt) &&
                DateTime.TryParse(payload.ScrapedAt, out var parsed))
            {
                publishedAt = parsed.ToUniversalTime();
            }

            return new CreateArticleRequest
            {
                Title = payload.Title ?? "Contenido sin título",
                Content = string.IsNullOrWhiteSpace(sanitizedText) ? payload.Text ?? string.Empty : sanitizedText,
                Author = "Redaction Team",
                PublishedAt = publishedAt,
                ImageUrl = "https://www.boynemedicalpractice.ie/wp-content/uploads/2021/10/placeholder-news.jpg",
                RelevanceScore = 1,
                IsPremium = false,
                CategoryNames = ExtractCategoryNames(payload.Topic)
            };
        }

        private static IReadOnlyCollection<string> ExtractCategoryNames(string? topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                return Array.Empty<string>();
            }

            var normalized = topic.Trim();

            var primaryFragments = normalized
                .Split(new[] { ',', '/', '|', '>', '-', ':' }, StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(fragment => fragment
                    .Split(new[] { " and ", "&", " + ", ";", Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                .Select(fragment => fragment.Trim())
                .Where(fragment => !string.IsNullOrWhiteSpace(fragment))
                .Select(fragment =>
                {
                    var lower = fragment.ToLowerInvariant();
                    return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(lower);
                })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (primaryFragments.Length > 0)
            {
                return primaryFragments;
            }

            return new[] { CultureInfo.CurrentCulture.TextInfo.ToTitleCase(normalized.ToLowerInvariant()) };
        }

        private async Task<string> SanitizeArticleTextAsync(string originalText, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(originalText))
            {
                return originalText;
            }

            var client = _httpClientFactory.CreateClient("TextSanitizerApi");

            var request = new
            {
                model = "llama3.1:latest",
                prompt = @"Delete every reference to a news outlet and discard any malformed data or references to website layout.
Return only the cleaned news content.

Text:
" + originalText,
                stream = false
            };

            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromMinutes(2));
                var effectiveToken = timeoutCts.Token;

                using var response = await client.PostAsJsonAsync("api/generate", request, effectiveToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Sanitizer returned {Status}. Falling back to original text. Body: {Body}",
                        response.StatusCode,
                        await response.Content.ReadAsStringAsync(effectiveToken));
                    return originalText;
                }

                var payload = await response.Content.ReadFromJsonAsync<SanitizerResponse>(cancellationToken: effectiveToken);
                return payload?.Response ?? payload?.Result ?? originalText;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Sanitizer request timed out. Falling back to original text.");
                return originalText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sanitizer call failed. Falling back to original text.");
                return originalText;
            }
        }

        private sealed class SanitizerResponse
        {
            [JsonPropertyName("response")]
            public string? Response { get; set; }

            [JsonPropertyName("result")]
            public string? Result { get; set; }
        }

        private sealed class ScrapedArticlePayload
        {
            [JsonPropertyName("title")]
            public string? Title { get; set; }

            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonPropertyName("text")]
            public string? Text { get; set; }

            [JsonPropertyName("source")]
            public string? Source { get; set; }

            [JsonPropertyName("scraped_at")]
            public string? ScrapedAt { get; set; }

            [JsonPropertyName("topic")]
            public string? Topic { get; set; }

            [JsonPropertyName("isCleaned")]
            public bool IsCleaned { get; set; }

            [JsonPropertyName("sentiment")]
            public SentimentPayload? Sentiment { get; set; }
        }

        private sealed class SentimentPayload
        {
            [JsonPropertyName("label")]
            public string? Label { get; set; }

            [JsonPropertyName("score")]
            public double? Score { get; set; }
        }
    }
}
