using DatingClickerServerApp.Common.Configuration;
using DatingClickerServerApp.Common.Model;
using DatingClickerServerApp.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DatingClickerServerApp.Core.Services
{
    public class DatingClickerApiService : IDatingClickerApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DatingClickerApiService> _logger;
        private readonly DatingClickerApiSettings _settings;

        private string _token = string.Empty;

        public DatingClickerApiService(
            IHttpClientFactory httpClientFactory,
            ILogger<DatingClickerApiService> logger, 
            IOptions<DatingClickerApiSettings> settings)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<List<DatingUser>> GetRecommendedUsers(bool onlineOnly = true, CancellationToken cancellationToken = default)
        {
            var content = CreateMultipartContent(
            [
                ("count", "50"),
                ("city_id", _settings.CityId.ToString() ),
                ("_token", _token)
            ]);

            var response = await SendRequest("dating.getRecommendedUsers", content, cancellationToken);

            var usersJson = JsonDocument.Parse(response);
            var users = usersJson.RootElement.GetProperty("users").EnumerateArray().AsEnumerable();

            if (onlineOnly)
            {
                users = users.Where(user => user.GetProperty("is_online").GetBoolean());
            }

            var datingUsers = users.Select(user => MapToDatingUser(user)).ToList();

            return datingUsers;
        }

        public async Task<DatingUser> GetRecommendedUser(string externalId, CancellationToken cancellationToken = default)
        {
            var content = CreateMultipartContent(
            [
                ("secure_user_id", externalId),
                ("_token", _token)
            ]);

            var response = await SendRequest("dating.getRecommendedUser", content, cancellationToken);

            var user = JsonDocument.Parse(response).RootElement.GetProperty("result");

            return MapToDatingUser(user);
        }

        public bool IsUserLikeable(DatingUser user, DatingUserCriteriesSettings datingUserCriteriesInfo)
        {
            var exclusionWords = datingUserCriteriesInfo.ExclusionWords ?? [];
            var requiredInterests = datingUserCriteriesInfo.Interests ?? [];

            var lastActiveAt = user.JsonData.GetProperty("last_active_at").GetDateTime();
            var oneWeekAgo = DateTime.UtcNow.AddDays(-7);

            return lastActiveAt >= oneWeekAgo
                && (!user.Height.HasValue || user.Height >= datingUserCriteriesInfo.Height)
                && (!datingUserCriteriesInfo.IsVerified.HasValue || user.IsVerified == datingUserCriteriesInfo.IsVerified)
                && (!datingUserCriteriesInfo.HasChildren.HasValue || !user.HasChildren.HasValue || user.HasChildren == datingUserCriteriesInfo.HasChildren)
                && (exclusionWords.Count == 0 || !exclusionWords.Any(word => user.About.Contains(word, StringComparison.OrdinalIgnoreCase)))
                && (requiredInterests.Count == 0 || requiredInterests.Any(interest => user.Interests.Contains(interest, StringComparer.OrdinalIgnoreCase)));
        }

        public bool IsUserSuperLikeable(DatingUser user, DatingUserCriteriesSettings datingUserCriteriesInfo)
        {
            var exclusionWords = datingUserCriteriesInfo.ExclusionWords ?? [];
            var requiredInterests = datingUserCriteriesInfo.Interests ?? [];

            var lastActiveAt = user.JsonData.GetProperty("last_active_at").GetDateTime();
            var oneWeekAgo = DateTime.UtcNow.AddDays(-7);

            return lastActiveAt >= oneWeekAgo
                && (!user.Height.HasValue || user.Height >= datingUserCriteriesInfo.Height)
                && (!datingUserCriteriesInfo.IsVerified.HasValue || user.IsVerified == datingUserCriteriesInfo.IsVerified)
                && (!datingUserCriteriesInfo.HasChildren.HasValue || user.HasChildren == datingUserCriteriesInfo.HasChildren)
                && (exclusionWords.Count == 0 || !exclusionWords.Any(word => user.About.Contains(word, StringComparison.OrdinalIgnoreCase)))
                && (requiredInterests.Count == 0 || requiredInterests.Any(interest => user.Interests.Contains(interest, StringComparer.OrdinalIgnoreCase)));
        }

        public async Task<string> LikeUser(string userId, CancellationToken cancellationToken = default)
        {
            var content = CreateMultipartContent(
            [
                ("user_id", userId),
                ("_token", _token)
            ]);

            return await SendRequest("dating.like", content, cancellationToken);
        }

        public async Task<string> DislikeUser(string userId, CancellationToken cancellationToken = default)
        {
            var content = CreateMultipartContent(
            [
                ("user_id", userId),
                ("_token", _token)
            ]);

            return await SendRequest("dating.dislike", content, cancellationToken);
        }

        public async Task<string> SuperLikeUser(string userId, string superLikeText = null, CancellationToken cancellationToken = default)
        {
            var content = CreateMultipartContent(
            [
                ("user_id", userId),
                ("_token", _token),
                ("super_like", $"{{\"text\":\"{superLikeText ?? string.Empty}\"}}")
            ]);

            return await SendRequest("dating.like", content, cancellationToken);
        }

        public async Task<DatingAppUser> SignIn(IDictionary<string, string> signInSettings, CancellationToken cancellationToken = default)
        {
            var authParams = await GetAuthParams(signInSettings, cancellationToken);

            return await AuthSignIn($"?{authParams}", cancellationToken);
        }

        private async Task<string> GetAuthParams(IDictionary<string, string> signInSettings, CancellationToken cancellationToken = default)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var client = _httpClientFactory.CreateClient();

            client.BaseAddress = new Uri(_settings.AuthBaseUrl);

            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            client.DefaultRequestHeaders.Add("Accept-Language", "ru,en;q=0.9,uk;q=0.8,pl;q=0.7");
            client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 YaBrowser/24.7.0.0 Safari/537.36");

            client.DefaultRequestHeaders.Add("Cookie", string.Join("; ", signInSettings.Select(kv => $"{kv.Key}={kv.Value}")));

            var response = await client.GetAsync("/dating", cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var match = Regex.Match(responseBody, _settings.AuthParametersSearchPattern);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            else
            {
                throw new InvalidOperationException("Строка с AuthParams не найдена.");
            }
        }

        private async Task<DatingAppUser> AuthSignIn(string launchUrl, CancellationToken cancellationToken = default)
        {
            var content = CreateMultipartContent(new[]
            {
                ("launch_url", launchUrl),
            });

            var response = await SendRequest("auth.signIn", content, cancellationToken);

            var signInJson = JsonDocument.Parse(response);
            _token = signInJson.RootElement.GetProperty("token").GetString();

            return new DatingAppUser
            {
                UserId = signInJson.RootElement.GetProperty("user").GetProperty("id").GetInt32().ToString(),
                SuperLikeCount = signInJson.RootElement.GetProperty("user").GetProperty("super_like_count").GetInt32(),
                JsonData = signInJson.RootElement
            };
        }

        private async Task<string> SendRequest(string endpoint, MultipartFormDataContent content, CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient();

            client.BaseAddress = new Uri(_settings.ApiBaseUrl);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content };
                var response = await client.SendAsync(request, cancellationToken);

                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                LogJsonResponse(endpoint, responseBody);

                return responseBody;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to {Endpoint} failed.", endpoint);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in {Endpoint} request.", endpoint);
                throw;
            }
        }

        private void LogJsonResponse(string endpoint, string jsonResponse)
        {
            var formattedJson = JsonSerializer.Serialize(JsonDocument.Parse(jsonResponse), new JsonSerializerOptions { WriteIndented = true });

            _logger.LogInformation("Response from {Endpoint}: {Json}", endpoint, formattedJson);
        }

        private static DatingUser MapToDatingUser(JsonElement user) => new()
        {
            ExternalId = user.GetProperty("id").GetInt32().ToString(),
            Name = user.GetProperty("name").GetString(),
            IsVerified = user.GetProperty("is_verify").GetBoolean(),
            Age = user.TryGetProperty("age", out var age) ? age.GetInt32() : null,
            HasChildren = user.GetProperty("form").TryGetProperty("kids", out var kids) ? kids.GetString() == "yes" : null,
            Height = user.GetProperty("form").TryGetProperty("height", out var height) ? height.GetInt32() : null,
            PreviewUrl = new Uri(user.GetProperty("preview_url").ToString()),
            About = user.GetProperty("form").GetProperty("about").GetString(),
            Interests = user.GetProperty("form").GetProperty("interests").EnumerateArray().Select(i => i.GetString()).ToArray(),
            CityName = user.TryGetProperty("city_name", out var cityName) ? cityName.ToString() : null,
            JsonData = user
        };

        private static MultipartFormDataContent CreateMultipartContent((string, string)[] parameters)
        {
            var content = new MultipartFormDataContent();

            foreach (var (name, value) in parameters)
            {
                content.Add(new StringContent(value), name);
            }

            return content;
        }
    }
}
