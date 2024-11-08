using DatingClickerServerApp.Common.Configuration;
using DatingClickerServerApp.Common.Extensions;
using DatingClickerServerApp.Common.Model;
using DatingClickerServerApp.Core.Interfaces;
using DatingClickerServerApp.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DatingClickerServerApp.Common
{
    public class DatingClickerProcessor
    {
        private readonly IDatingClickerService _datingClickerService;
        private readonly DatingClickerProcessorSettings _settings;
        private readonly AppDbContext _dbContext;
        private readonly IEncryptionService _encryptionService;

        public event Func<string, Task> OnResultUpdated;

        public DatingClickerProcessor(
            IDatingClickerService datingClickerService,
            DatingClickerProcessorSettings settings,
            AppDbContext dbContext,
            IEncryptionService encryptionService)
        {
            _datingClickerService = datingClickerService;
            _settings = settings;
            _dbContext = dbContext;
            _encryptionService = encryptionService;
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

                            var (result, actionType) = await DetermineAction(datingUser, user.Item1, superLikeCriteries, _settings.LikeCriteries, isEndOfDayApproaching, isUserEnoughSuperLikeCount, ++counter, cancellationToken);

                            processedUserIds.Add(datingUser.ExternalId);

                            await SaveDatingUser(datingUser, actionType, user.Item2.Id, cancellationToken);

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

        private async Task<(string result, DatingUserActionType actionType)> DetermineAction(
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

            var existingUser = await _dbContext.DatingUsers
                .Include(u => u.Actions)
                .Include(u => u.BlacklistedDatingUser)
                .FirstOrDefaultAsync(u => u.ExternalId == datingUser.ExternalId, cancellationToken);

            if (existingUser != null)
            {
                if (existingUser.BlacklistedDatingUser != null)
                {
                    result = $"Dislike (Blacklisted): {await _datingClickerService.DislikeUser(datingUser.ExternalId, cancellationToken)}, {datingUser.CityName}, {counter} {(datingUser.IsVerified ? "✓" : string.Empty)}\n";
                    
                    return (result, DatingUserActionType.Dislike);
                }

                var userHasExistingSuperLikeAction = existingUser.Actions
                    .Any(a => a.ActionType == DatingUserActionType.SuperLike);

                if (userHasExistingSuperLikeAction)
                {
                    await _datingClickerService.DislikeUser(datingUser.ExternalId, cancellationToken);

                    return ($"Super Like already exists for user: {datingUser.ExternalId}\n", DatingUserActionType.Dislike);
                }
            }

            if (_datingClickerService.IsUserSuperLikeable(datingUser, datingUserSuperLikeCriteriesSettings))
            {
                if (isUserEnoughSuperLikeCount)
                {
                    string superLikeText = !isEndOfDayApproaching
                        ? "Если правда увлекаешься или работаешь в ИТ, то удачи тебе в развитии в нашей нелегкой ИТ-сфере 😊"
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

            return (result, actionType);
        }

        private async Task<Tuple<DatingAppUser, DatingAccount>> SignIn(CancellationToken cancellationToken)
        {
            var user = await _datingClickerService.SignIn(_settings.SignIn, cancellationToken);

            var dbDatingAccount = await _dbContext.DatingAccounts.FirstOrDefaultAsync(da => da.AppUserId == user.UserId, cancellationToken);

            if (dbDatingAccount != null)
            {
                dbDatingAccount.UpdatedDate = DateTime.UtcNow;
                dbDatingAccount.JsonProfileData = user.JsonData;

                _dbContext.DatingAccounts.Update(dbDatingAccount);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                var datingAccount = new DatingAccount
                {
                    AppUserId = user.UserId,
                    AppName = DatingAppNameType.VkDating,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    JsonAuthData = _encryptionService.Encrypt(JsonSerializer.Serialize(_settings.SignIn)),
                    JsonProfileData = user.JsonData
                };

                var entityEntry = await _dbContext.DatingAccounts.AddAsync(datingAccount, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                dbDatingAccount = entityEntry.Entity;
            }

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

        private async Task SaveDatingUser(DatingUser datingUser, DatingUserActionType actionType, Guid datingAccountId, CancellationToken cancellationToken)
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
                        ActionType = actionType,
                        DatingAccountId = datingAccountId
                    }
                ];

                await _dbContext.DatingUsers.AddAsync(datingUser, cancellationToken);
            }
            else
            {
                dbDatingUser.UpdatedDate = DateTime.UtcNow;
                dbDatingUser.Name = datingUser.Name;
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
                    DatingUserId = dbDatingUser.Id,
                    DatingAccountId = datingAccountId
                }, cancellationToken);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
