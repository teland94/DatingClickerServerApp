using DatingClickerServerApp.Common.Model;
using DatingClickerServerApp.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DatingClickerServerApp.Common.Services
{
    public class VkDatingClickerService : IDatingClickerService
    {
        private readonly HttpClient _client;

        private string _token = string.Empty;

        public VkDatingClickerService(AppDbContext dbContext)
        {
            _client = new HttpClient();
        }

        public async Task<List<DatingUser>> GetRecommendedUsers(bool onlineOnly = true, CancellationToken cancellationToken = default)
        {
            var usersRequest = new HttpRequestMessage(HttpMethod.Post, "https://dating.vk.com/api/dating.getRecommendedUsers")
            {
                Content = CreateMultipartContent(
                [
                    ("count", "50"),
                    ("city_id", "1"),
                    ("_token", _token)
                ])
            };

            var usersResponse = await _client.SendAsync(usersRequest, cancellationToken);
            var usersResponseString = await usersResponse.Content.ReadAsStringAsync(cancellationToken);

            // Красивый вывод JSON
            var usersJsonFormatted = JsonSerializer.Serialize(JsonDocument.Parse(usersResponseString), new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            Console.WriteLine("Users Response: " + usersJsonFormatted);

            // Парсим ответ и фильтруем только онлайн пользователей, если onlineOnly = true
            var usersJson = JsonDocument.Parse(usersResponseString);
            var users = usersJson.RootElement.GetProperty("users").EnumerateArray().AsEnumerable();

            if (onlineOnly)
            {
                users = users.Where(user => user.GetProperty("is_online").GetBoolean());
            }

            var datingUsers = users.Select(user => new DatingUser
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
            }).ToList();

            return datingUsers;
        }

        public async Task<DatingUser> GetRecommendedUser(string externalId, CancellationToken cancellationToken = default)
        {
            var usersRequest = new HttpRequestMessage(HttpMethod.Post, "https://dating.vk.com/api/dating.getRecommendedUser")
            {
                Content = CreateMultipartContent(
                [
                    ("secure_user_id", externalId),
                    ("_token", _token)
                ])
            };

            var userResponse = await _client.SendAsync(usersRequest, cancellationToken);
            var userResponseString = await userResponse.Content.ReadAsStringAsync(cancellationToken);

            var usersJson = JsonDocument.Parse(userResponseString);
            var user = usersJson.RootElement.GetProperty("result");

            var datingUser = new DatingUser
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

            return datingUser;
        }

        //Лайк: рост 155 и выше, без детей (!Есть дети), исключить из описания заданные ключевые слова, исключить анкеты без активности в течение недели
        public bool IsUserLikeable(DatingUser user)
        {
            string[] exclusionWords = ["plus size", "plus-size", "мужчину для кое чего интересного", "которому смогу показать себя"];

            var lastActiveAt = user.JsonData.GetProperty("last_active_at").GetDateTime();
            var oneWeekAgo = DateTime.UtcNow.AddDays(-7);

            return (!user.Height.HasValue || user.Height >= 155)
                && user.HasChildren != true
                && !exclusionWords.Any(word => user.About.Contains(word, StringComparison.OrdinalIgnoreCase))
                && lastActiveAt >= oneWeekAgo;
        }

        public bool IsUserSuperLikeable(DatingUser user, DatingUserCriteriesInfo datingUserCriteriesInfo = null)
        {
            var requiredInterests = datingUserCriteriesInfo?.Interests ?? [];

            return user.Height >= (datingUserCriteriesInfo?.Height ?? 165)
                && user.IsVerified
                && (requiredInterests.Count == 0 || requiredInterests.Any(interest => user.Interests.Contains(interest, StringComparer.OrdinalIgnoreCase)))
                && IsUserLikeable(user);
        }

        public async Task<string> LikeUser(string userId, CancellationToken cancellationToken = default)
        {
            var likeRequest = new HttpRequestMessage(HttpMethod.Post, "https://dating.vk.com/api/dating.like")
            {
                Content = CreateMultipartContent(
                [
                    ("user_id", userId),
                    ("_token", _token)
                ])
            };

            var likeResponse = await _client.SendAsync(likeRequest, cancellationToken);
            var likeResponseString = await likeResponse.Content.ReadAsStringAsync(cancellationToken);

            // Красивый вывод JSON
            var likeJsonFormatted = JsonSerializer.Serialize(JsonDocument.Parse(likeResponseString), new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            Console.WriteLine("Like Response for user " + userId + ": " + likeJsonFormatted);

            return likeJsonFormatted;
        }

        public async Task<string> DislikeUser(string userId, CancellationToken cancellationToken = default)
        {
            var dislikeRequest = new HttpRequestMessage(HttpMethod.Post, "https://dating.vk.com/api/dating.dislike")
            {
                Content = CreateMultipartContent(
                [
                    ("user_id", userId),
                    ("_token", _token)
                ])
            };

            var dislikeResponse = await _client.SendAsync(dislikeRequest, cancellationToken);
            var dislikeResponseString = await dislikeResponse.Content.ReadAsStringAsync(cancellationToken);

            // Красивый вывод JSON
            var dislikeJsonFormatted = JsonSerializer.Serialize(JsonDocument.Parse(dislikeResponseString), new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            Console.WriteLine("Dislike Response for user " + userId + ": " + dislikeJsonFormatted);

            return dislikeJsonFormatted;
        }

        public async Task<string> SuperLikeUser(string userId, string superLikeText = null, CancellationToken cancellationToken = default)
        {
            var likeRequest = new HttpRequestMessage(HttpMethod.Post, "https://dating.vk.com/api/dating.like")
            {
                Content = CreateMultipartContent(
                [
                    ("user_id", userId),
                    ("_token", _token),
                    ("super_like", $"{{\"text\":\"{(!string.IsNullOrEmpty(superLikeText) ? superLikeText : string.Empty)}\"}}")
                ])
            };

            var likeResponse = await _client.SendAsync(likeRequest, cancellationToken);
            var likeResponseString = await likeResponse.Content.ReadAsStringAsync(cancellationToken);

            // Красивый вывод JSON
            var likeJsonFormatted = JsonSerializer.Serialize(JsonDocument.Parse(likeResponseString), new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            Console.WriteLine("Like Response for user " + userId + ": " + likeJsonFormatted);

            return likeJsonFormatted;
        }

        public async Task<User> SignIn(IDictionary<string, string> signInSettings, CancellationToken cancellationToken)
        {
            var vkParams = await GetVkParams(signInSettings["p"], signInSettings["remixsid"], cancellationToken);

            return await AuthSignIn($"?{vkParams}", cancellationToken);
        }

        private static async Task<string> GetVkParams(string p, string remixsid, CancellationToken cancellationToken = default)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var handler = new HttpClientHandler
            {
                CookieContainer = new CookieContainer()
            };

            using HttpClient client = new(handler);

            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            client.DefaultRequestHeaders.Add("Accept-Language", "ru,en;q=0.9,uk;q=0.8,pl;q=0.7");
            client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 YaBrowser/24.7.0.0 Safari/537.36");

            handler.CookieContainer.Add(new Uri("https://login.vk.com"), new Cookie("p", p));
            handler.CookieContainer.Add(new Uri("https://vk.com"), new Cookie("remixsid", remixsid));

            HttpResponseMessage response = await client.GetAsync("https://vk.com/dating", cancellationToken);

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            var pattern = @"var vkParams = '([^']*)';";
            var match = Regex.Match(responseBody, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            else
            {
                throw new InvalidOperationException("Строка vkParams не найдена.");
            }
        }

        private async Task<User> AuthSignIn(string launchUrl, CancellationToken cancellationToken = default)
        {
            var signInRequest = new HttpRequestMessage(HttpMethod.Post, "https://dating.vk.com/api/auth.signIn")
            {
                Content = CreateMultipartContent(
                [
                    ("launch_url", launchUrl),
                ])
            };

            var signInResponse = await _client.SendAsync(signInRequest, cancellationToken);

            signInResponse.EnsureSuccessStatusCode();

            var signInResponseString = await signInResponse.Content.ReadAsStringAsync(cancellationToken);

            // Красивый вывод JSON
            var signInJsonFormatted = JsonSerializer.Serialize(JsonDocument.Parse(signInResponseString), new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            Console.WriteLine("SignIn Response: " + signInJsonFormatted);

            // Извлечение токена из ответа
            var signInJson = JsonDocument.Parse(signInResponseString);
            _token = signInJson.RootElement.GetProperty("token").GetString();

            return new User
            {
                SuperLikeCount = signInJson.RootElement.GetProperty("user").GetProperty("super_like_count").GetInt32()
            };
        }

        private MultipartFormDataContent CreateMultipartContent((string, string)[] parameters)
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