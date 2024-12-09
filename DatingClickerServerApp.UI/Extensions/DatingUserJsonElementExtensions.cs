using DatingClickerServerApp.Common.Extensions;
using System.Globalization;
using System.Text.Json;

namespace DatingClickerServerApp.UI.Extensions
{
    public static class DatingUserJsonElementExtensions
    {
        public static string ExtractDistance(this JsonElement jsonData)
        {
            if (jsonData.TryGetProperty("extra", out JsonElement extraElement) &&
                extraElement.TryGetProperty("distance", out JsonElement distanceElement))
            {
                int distanceInMeters = distanceElement.GetInt32();
                int distanceInKilometers = (int)Math.Round(distanceInMeters / 1000.0);

                return $"{distanceInKilometers} км";
            }

            return "Неизвестно";
        }

        public static string ExtractShareUrl(this JsonElement jsonData)
        {
            return jsonData.TryGetProperty("share_url", out var shareUrl) ? shareUrl.GetString() : null;
        }

        public static string ExtractLastActiveAt(this JsonElement jsonData)
        {
            if (jsonData.TryGetProperty("last_active_at", out JsonElement lastActiveElement) &&
                lastActiveElement.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(lastActiveElement.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime dateTime))
            {
                return dateTime.ConvertToLocalTime().ToString("dd.MM.yyyy HH:mm");
            }

            return "Неизвестно";
        }
    }
}
