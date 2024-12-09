using DatingClickerServerApp.Common.Configuration;
using DatingClickerServerApp.Common.Extensions;
using DatingClickerServerApp.Common.Model;
using DatingClickerServerApp.Core.Interfaces;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace DatingClickerServerApp.Core
{
    public class DatingClickerProcessor
    {
        private readonly IDatingClickerApiService _datingClickerService;
        private readonly DatingClickerProcessorSettings _settings;
        private readonly IDatingUserService _datingUserService;
        private readonly IDatingAccountService _datingAccountService;
        private readonly OpenAIClient _openAIClient;

        public event Func<string, Task> OnResultUpdated;

        public DatingClickerProcessor(
            IDatingClickerApiService datingClickerService,
            IOptions<DatingClickerProcessorSettings> settings,
            IDatingUserService datingUserService,
            IDatingAccountService datingAccountService,
            OpenAIClient openAiClient)
        {
            _datingClickerService = datingClickerService;
            _settings = settings.Value;
            _datingUserService = datingUserService;
            _datingAccountService = datingAccountService;
            _openAIClient = openAiClient;   
        }

        public async Task ProcessDatingUsers(bool onlineOnly, int repeatCount, CancellationToken cancellationToken = default)
        {
            var random = new Random();
            var processedUserIds = new HashSet<string>();

            try
            {
                var user = await SignIn(cancellationToken);

                var isEndOfDayApproaching = IsEndOfDayApproaching();
                var isUserEnoughSuperLikeCount = user.Item1.SuperLikeCount > 0;
                var superLikeCriteries = GetSuperLikeCriteries(isUserEnoughSuperLikeCount, isEndOfDayApproaching);

                for (int i = 0; i < repeatCount; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var datingUsers = await _datingClickerService.GetRecommendedUsers(onlineOnly, cancellationToken);

                    var newDatingUsers = datingUsers.Where(du => !processedUserIds.Contains(du.ExternalId)).ToList();

                    if (newDatingUsers.Count > 0)
                    {
                        var counter = 0;

                        foreach (var datingUser in newDatingUsers)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var (result, actionType, superLikeText) = await DetermineAction(datingUser, user.Item1, superLikeCriteries, _settings.LikeCriteries, isEndOfDayApproaching, isUserEnoughSuperLikeCount, ++counter, cancellationToken);

                            processedUserIds.Add(datingUser.ExternalId);

                            await _datingUserService.SaveDatingUser(datingUser, actionType, superLikeText, user.Item2.Id, cancellationToken);

                            OnResultUpdated?.Invoke(result);

                            await RandomDelay(random, 1000, 5000, cancellationToken);
                        }
                    }
                    else
                    {
                        OnResultUpdated?.Invoke(onlineOnly ? "Не найдено онлайн пользователей.\n" : "Не найдено пользователей.\n");
                        break;
                    }

                    OnResultUpdated?.Invoke("================================\n");

                    await RandomDelay(random, 2000, 7000, cancellationToken);
                }

                OnResultUpdated?.Invoke("Обработка завершена.\n");
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                OnResultUpdated?.Invoke(ex.Message);
                throw;
            }
        }

        private async Task<(string result, DatingUserActionType actionType, string superLikeText)> DetermineAction(
            DatingUser datingUser,
            DatingAppUser user,
            DatingUserCriteriesSettings datingUserSuperLikeCriteriesSettings,
            DatingUserCriteriesSettings datingUserLikeCriteriesSettings,
            bool isEndOfDayApproaching,
            bool isUserEnoughSuperLikeCount,
            int counter,
            CancellationToken cancellationToken)
        {
            string result;
            DatingUserActionType actionType;
            string superLikeText = null;

            var existingUser = await _datingUserService.GetUserByExternalId(datingUser.ExternalId, cancellationToken);

            var test = await GenerateSuperLikeText(datingUser, isEndOfDayApproaching, cancellationToken);

            if (existingUser != null)
            {
                if (existingUser.BlacklistedDatingUser != null)
                {
                    result = $"Dislike (Blacklisted): {await _datingClickerService.DislikeUser(datingUser.ExternalId, cancellationToken)}, {datingUser.CityName}, {counter} {(datingUser.IsVerified ? "✓" : string.Empty)}\n";

                    return (result, DatingUserActionType.Dislike, null);
                }

                if (_settings.DislikeIfUserHasExistingSuperLikeAction)
                {
                    var userHasExistingSuperLikeAction = existingUser.Actions
                        .Any(a => a.ActionType == DatingUserActionType.SuperLike);

                    if (userHasExistingSuperLikeAction)
                    {
                        await _datingClickerService.DislikeUser(datingUser.ExternalId, cancellationToken);

                        return ($"Super Like already exists for user: {datingUser.ExternalId}\n", DatingUserActionType.Dislike, null);
                    }
                }
            }

            if (_datingClickerService.IsUserSuperLikeable(datingUser, datingUserSuperLikeCriteriesSettings))
            {
                if (isUserEnoughSuperLikeCount)
                {
                    if (_settings.UseChatBot)
                    {
                        superLikeText = await GenerateSuperLikeText(datingUser, isEndOfDayApproaching, cancellationToken);

                        if (superLikeText.Length > 140)
                        {
                            superLikeText = string.Concat(superLikeText.AsSpan(0, 137), "...");
                        }
                    }
                    else
                    {
                        superLikeText = !isEndOfDayApproaching
                            ? "Если правда увлекаешься или работаешь в ИТ, то удачи тебе в развитии в нашей нелегкой ИТ-сфере 😊"
                            : $"Привет, твой рост {datingUser.Height} см хорош! 😊";
                    }

                    result = $"Super Like: {await _datingClickerService.SuperLikeUser(datingUser.ExternalId, superLikeText, cancellationToken)}, {datingUser.CityName}, {counter} {(datingUser.IsVerified ? "✓" : string.Empty)}\n";

                    user.SuperLikeCount--;
                    actionType = DatingUserActionType.SuperLike;
                }
                else
                {
                    result = "None";
                    actionType = DatingUserActionType.None;
                }
            }
            else if (_datingClickerService.IsUserLikeable(datingUser, datingUserLikeCriteriesSettings))
            {
                result = $"Like: {await _datingClickerService.LikeUser(datingUser.ExternalId, cancellationToken)}, {datingUser.CityName}, {counter} {(datingUser.IsVerified ? "✓" : string.Empty)}\n";
                actionType = DatingUserActionType.Like;
            }
            else
            {
                result = $"Dislike: {await _datingClickerService.DislikeUser(datingUser.ExternalId, cancellationToken)}, Дети: {datingUser.HasChildren}, Возраст: {datingUser.Age}, Рост: {datingUser.Height}, {datingUser.CityName}, {counter} {(datingUser.IsVerified ? "✓" : string.Empty)}\n";
                actionType = DatingUserActionType.Dislike;
            }

            return (result, actionType, superLikeText);
        }

        private async Task<Tuple<DatingAppUser, DatingAccount>> SignIn(CancellationToken cancellationToken)
        {
            var user = await _datingClickerService.SignIn(_settings.SignIn, cancellationToken);

            var dbDatingAccount = await _datingAccountService.SaveDatingAccount(user, _settings.SignIn, cancellationToken);

            return new Tuple<DatingAppUser, DatingAccount>(user, dbDatingAccount);
        }

        private static async Task RandomDelay(Random random, int minMilliseconds, int maxMilliseconds, CancellationToken cancellationToken)
        {
            await Task.Delay(random.Next(minMilliseconds, maxMilliseconds), cancellationToken);
        }

        private DatingUserCriteriesSettings GetSuperLikeCriteries(bool isUserEnoughSuperLikeCount, bool isSmoothCriteries = false)
        {
            if (!isSmoothCriteries || !isUserEnoughSuperLikeCount)
            {
                //Обычные критерии
                return new DatingUserCriteriesSettings(165, ["IT"], _settings.LikeCriteries.ExclusionWords ?? [], true, false);
            }
            else
            {
                //Смягченные критерии
                return new DatingUserCriteriesSettings(170, [], _settings.LikeCriteries.ExclusionWords ?? [], true, false); // Убираем требование по интересам
            }
        }

        private async Task<string> GenerateSuperLikeText(DatingUser datingUser, bool isEndOfDayApproaching, CancellationToken cancellationToken)
        {
            var prompt = isEndOfDayApproaching
                ? $"{datingUser.JsonData} Напиши текст суперлайка, который включает комплимент роста {datingUser.Height} см очень коротко до 140 символов. Девушка незнакомая. Встречу сразу не предлагай, чашечку кофе тоже, и не упоминай вообще."
                : $"{datingUser.JsonData} Напиши текст суперлайка для пользователя очень коротко до 140 символов, который работает в сфере IT. Девушка незнакомая. Встречу сразу не предлагай, чашечку кофе тоже, и не упоминай вообще.";

            var chatRequest = new ChatRequest([ new Message(Role.User, prompt) ]);

            var response = await _openAIClient.ChatEndpoint.GetCompletionAsync(chatRequest, cancellationToken);
            var choice = response.FirstChoice;

            return choice.Message;
        }

        private static bool IsEndOfDayApproaching()
        {
            // Получаем текущее время по UTC
            var currentTime = DateTime.UtcNow;

            // Получаем начало следующего дня по локальному времени
            var startOfNextDay = currentTime.AddDays(1).GetStartOfDay(false);

            // Вычисляем время за один час до конца текущего дня
            var oneHourBeforeEndOfDay = startOfNextDay.AddHours(-1);

            return currentTime.ConvertToLocalTime() >= oneHourBeforeEndOfDay;
        }
    }
}
