using DatingClickerServerApp.Common.Extensions;
using DatingClickerServerApp.Common.Model;
using DatingClickerServerApp.Common.Persistence;
using DatingClickerServerApp.Common.Services;
using Microsoft.EntityFrameworkCore;

namespace DatingClickerServerApp.Common
{
    public class DatingClickerProcessor
    {
        private readonly IDatingClickerService _datingClickerService;
        private readonly IDictionary<string, string> _signInSettings;
        private readonly AppDbContext _dbContext;

        public event Func<string, Task> OnResultUpdated;

        public DatingClickerProcessor(
            IDatingClickerService datingClickerService,
            IDictionary<string, string> signInSettings,
            AppDbContext dbContext)
        {
            _datingClickerService = datingClickerService;
            _signInSettings = signInSettings;
            _dbContext = dbContext;
        }

        public async Task ProcessDatingUsers(bool onlineOnly, int repeatCount, CancellationToken cancellationToken)
        {
            var random = new Random();
            string result;

            try
            {
                var user = await _datingClickerService.SignIn(_signInSettings, cancellationToken);

                var isEndOfDayApproaching = IsEndOfDayApproaching();
                var isUserEnoughSuperLikeCount = user.SuperLikeCount > 0;
                var superLikeCriteries = GetSuperLikeCriteries(isUserEnoughSuperLikeCount, isEndOfDayApproaching);

                for (int i = 0; i < repeatCount; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var datingUsers = await _datingClickerService.GetRecommendedUsers(onlineOnly, cancellationToken);

                    if (datingUsers.Count > 0)
                    {
                        var counter = 0;

                        foreach (var datingUser in datingUsers)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            DatingUserActionType datingUserActionType;

                            if (_datingClickerService.IsUserSuperLikeable(datingUser, superLikeCriteries))
                            {
                                if (isUserEnoughSuperLikeCount)
                                {
                                    string superLikeText;

                                    if (!isEndOfDayApproaching)
                                    {
                                        // Обычные критерии
                                        superLikeText = "Если правда увлекаешься ИТ, то удачи тебе в развитии в нашей нелегкой ИТ-сфере 😊";
                                    }
                                    else
                                    {
                                        // Смягченные критерии за час до конца дня
                                        superLikeText = $"Привет, твой рост {datingUser.Height} см хорош! 😊";
                                    }

                                    result = $"Super Like: {await _datingClickerService.SuperLikeUser(datingUser.ExternalId, superLikeText, cancellationToken)}, {datingUser.CityName}, {++counter} {(datingUser.IsVerified ? "✓" : string.Empty)}\n";

                                    user.SuperLikeCount--;
                                    datingUserActionType = DatingUserActionType.SuperLike;
                                }
                                else
                                {
                                    result = "None";

                                    datingUserActionType = DatingUserActionType.None;
                                }
                            }
                            else if (_datingClickerService.IsUserLikeable(datingUser))
                            {
                                result = $"Like: {await _datingClickerService.LikeUser(datingUser.ExternalId, cancellationToken)}, {datingUser.CityName}, {++counter} {(datingUser.IsVerified ? "✓" : string.Empty)}\n";

                                datingUserActionType = DatingUserActionType.Like;
                            }
                            else
                            {
                                result = $"Dislike: {await _datingClickerService.DislikeUser(datingUser.ExternalId, cancellationToken)}, Дети: {datingUser.HasChildren}, Возраст: {datingUser.Age}, Рост: {datingUser.Height}, {datingUser.CityName}, {++counter} {(datingUser.IsVerified ? "✓" : string.Empty)}\n";

                                datingUserActionType = DatingUserActionType.Dislike;
                            }

                            await SaveDatingUser(datingUser, datingUserActionType, cancellationToken);

                            OnResultUpdated?.Invoke(result);

                            // Асинхронная случайная задержка от 1 до 5 секунд
                            await Task.Delay(random.Next(1000, 5000), cancellationToken);
                        }
                    }
                    else
                    {
                        result = onlineOnly ? "Не найдено онлайн пользователей.\n" : "Не найдено пользователей.\n";
                        OnResultUpdated?.Invoke(result);
                        break;
                    }

                    result = "================================\n";
                    OnResultUpdated?.Invoke(result);

                    // Асинхронная случайная задержка от 2 до 7 секунд
                    await Task.Delay(random.Next(2000, 7000), cancellationToken);
                }

                result = "Обработка завершена.\n";
                OnResultUpdated?.Invoke(result);
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                result = ex.Message;
                OnResultUpdated?.Invoke(result);
                throw;
            }
        }

        private static DatingUserCriteriesInfo GetSuperLikeCriteries(bool isUserEnoughSuperLikeCount, bool isSmoothCriteries = false)
        {
            if (!isSmoothCriteries || !isUserEnoughSuperLikeCount)
            {
                // Обычные критерии
                return new DatingUserCriteriesInfo(165, ["IT"]);
            }
            else
            {
                // Смягченные критерии
                return new DatingUserCriteriesInfo(170, []); // Убираем требование по интересам
            }
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

        private async Task SaveDatingUser(DatingUser datingUser, DatingUserActionType actionType, CancellationToken cancellationToken)
        {
            var dbDatingUser = await _dbContext.DatingUsers.FirstOrDefaultAsync(du => du.ExternalId == datingUser.ExternalId, cancellationToken);

            if (dbDatingUser == null)
            {
                datingUser.CreatedDate = DateTime.UtcNow;

                datingUser.Actions =
                [
                    new DatingUserAction
                    {
                        CreatedDate = DateTime.UtcNow,
                        ActionType = actionType
                    }
                ];

                await _dbContext.DatingUsers.AddAsync(datingUser, cancellationToken);
            }
            else
            {
                await _dbContext.DatingUserActions.AddAsync(new DatingUserAction
                {
                    CreatedDate = DateTime.UtcNow,
                    ActionType = actionType,
                    DatingUserId = dbDatingUser.Id
                }, cancellationToken);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
