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

                            var (result, actionType) = await DetermineAction(datingUser, user, superLikeCriteries, isEndOfDayApproaching, isUserEnoughSuperLikeCount, ++counter, cancellationToken);

                            await SaveDatingUser(datingUser, actionType, cancellationToken);

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

        private async Task<(string result, DatingUserActionType actionType)> DetermineAction(DatingUser datingUser, User user, DatingUserCriteriesInfo superLikeCriteries, bool isEndOfDayApproaching, bool isUserEnoughSuperLikeCount, int counter, CancellationToken cancellationToken)
        {
            string result;
            DatingUserActionType actionType;

            var existingUser = await _dbContext.DatingUsers
                .Include(u => u.Actions)
                .FirstOrDefaultAsync(u => u.ExternalId == datingUser.ExternalId, cancellationToken);

            if (existingUser != null)
            {
                var userHasExistingSuperLikeAction = existingUser.Actions
                    .Any(a => a.ActionType == DatingUserActionType.SuperLike);

                if (userHasExistingSuperLikeAction)
                {
                    await _datingClickerService.DislikeUser(datingUser.ExternalId, cancellationToken);

                    return ($"Super Like already exists for user: {datingUser.ExternalId}\n", DatingUserActionType.Dislike);
                }
            }

            if (_datingClickerService.IsUserSuperLikeable(datingUser, superLikeCriteries))
            {
                if (isUserEnoughSuperLikeCount)
                {
                    string superLikeText = !isEndOfDayApproaching
                        ? "Если правда увлекаешься ИТ, то удачи тебе в развитии в нашей нелегкой ИТ-сфере 😊"
                        : $"Привет, твой рост {datingUser.Height} см хорош! 😊";

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
            else if (_datingClickerService.IsUserLikeable(datingUser))
            {
                result = $"Like: {await _datingClickerService.LikeUser(datingUser.ExternalId, cancellationToken)}, {datingUser.CityName}, {counter} {(datingUser.IsVerified ? "✓" : string.Empty)}\n";
                actionType = DatingUserActionType.Like;
            }
            else
            {
                result = $"Dislike: {await _datingClickerService.DislikeUser(datingUser.ExternalId, cancellationToken)}, Дети: {datingUser.HasChildren}, Возраст: {datingUser.Age}, Рост: {datingUser.Height}, {datingUser.CityName}, {counter} {(datingUser.IsVerified ? "✓" : string.Empty)}\n";
                actionType = DatingUserActionType.Dislike;
            }

            return (result, actionType);
        }

        private static async Task RandomDelay(Random random, int minMilliseconds, int maxMilliseconds, CancellationToken cancellationToken)
        {
            await Task.Delay(random.Next(minMilliseconds, maxMilliseconds), cancellationToken);
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
                datingUser.UpdatedDate = DateTime.UtcNow;

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
                dbDatingUser.UpdatedDate = DateTime.UtcNow;
                dbDatingUser.IsVerified = datingUser.IsVerified;
                dbDatingUser.Age = datingUser.Age;
                dbDatingUser.HasChildren = datingUser.HasChildren;
                dbDatingUser.Height = datingUser.Height;
                dbDatingUser.PreviewUrl = datingUser.PreviewUrl;
                dbDatingUser.About = datingUser.About;
                dbDatingUser.Interests = datingUser.Interests;
                dbDatingUser.CityName = datingUser.CityName;
                dbDatingUser.JsonData = datingUser.JsonData;

                _dbContext.DatingUsers.Update(dbDatingUser);

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
