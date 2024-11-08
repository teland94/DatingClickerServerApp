using DatingClickerServerApp.Common.Model;
using System.Collections.Immutable;

namespace DatingClickerServerApp.UI.Extensions
{
    public static class DatingUserActionTypeExtensions
    {
        private static readonly ImmutableDictionary<DatingUserActionType, string> DatingUserActionTypeNames = new Dictionary<DatingUserActionType, string>
        {
            { DatingUserActionType.None, "Нет действия ➖" },
            { DatingUserActionType.SuperLike, "Суперлайк 🔥" },
            { DatingUserActionType.Like, "Лайк ❤️" },
            { DatingUserActionType.Dislike, "Дизлайк ❌" }
        }.ToImmutableDictionary();

        private static readonly ImmutableDictionary<DatingUserActionType, string> DatingUserActionTypeShortNames = new Dictionary<DatingUserActionType, string>
        {
            { DatingUserActionType.None, "➖" },
            { DatingUserActionType.SuperLike, "🔥" },
            { DatingUserActionType.Like, "❤️" },
            { DatingUserActionType.Dislike, "❌" }
        }.ToImmutableDictionary();

        public static string GetName(this DatingUserActionType actionType) => DatingUserActionTypeNames.TryGetValue(actionType, out var name) ? name : string.Empty;

        public static string GetShortName(this DatingUserActionType actionType) => DatingUserActionTypeShortNames.TryGetValue(actionType, out var name) ? name : string.Empty;
    }
}
