using DatingClickerServerApp.Common.Extensions;
using DatingClickerServerApp.Common.Model;
using DatingClickerServerApp.Common.Persistence;
using DatingClickerServerApp.Common.Services;
using DatingClickerServerApp.Components.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using System.Text.Json;

namespace DatingClickerServerApp.Pages
{
    public partial class DatingUsers : ComponentBase
    {
        private readonly List<SelectableItem<DatingUserActionType>> _selectableAllActionTypes = Enum.GetValues<DatingUserActionType>()
            .Select(action => new SelectableItem<DatingUserActionType>
            {
                Item = action,
                IsSelected = false
            }).ToList();
        private readonly List<SelectableItem<DatingUserActionType>> _selectableLastActionTypes = Enum.GetValues<DatingUserActionType>()
            .Select(action => new SelectableItem<DatingUserActionType>
            {
                Item = action,
                IsSelected = false
            }).ToList();

        private List<DatingUser> _users;

        private bool _onlyVerified = false;
        private bool _onlyToday = true;
        private ICollection<DatingUserActionType> _selectedAllActionTypes = [];
        private ICollection<DatingUserActionType> _selectedLastActionTypes = [];
        private string _searchText = string.Empty;

        private int _currentPage = 1;
        private int _pageSize = 100;
        private int _totalUsers;
        private int _totalPages;

        private DatingUser _selectedUser;

        [Inject] private IDbContextFactory<AppDbContext> DbContextFactory { get; set; }
        [Inject] private IDatingClickerService DatingClickerService { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadUsers();
        }

        private async Task LoadUsers()
        {
            using var dbContext = DbContextFactory.CreateDbContext();

            IQueryable<DatingUser> query = dbContext.DatingUsers
                .Include(u => u.Actions.OrderByDescending(a => a.CreatedDate))
                .Include(u => u.BlacklistedDatingUser);

            if (_onlyVerified)
            {
                query = query.Where(u => u.IsVerified);
            }

            if (_onlyToday)
            {
                var startOfToday = DateTime.UtcNow.GetStartOfDay();
                query = query.Where(u => u.UpdatedDate >= startOfToday);
            }

            if (_selectedAllActionTypes.Count != 0)
            {
                query = query.Where(u => u.Actions.Any(a => _selectedAllActionTypes.Any(sa => sa == a.ActionType)));
            }

            if (_selectedLastActionTypes.Count != 0)
            {
                query = query.Where(u => _selectedLastActionTypes.Any(sa => sa == u.Actions.First().ActionType));
            }

            if (!string.IsNullOrEmpty(_searchText))
            {
                var lowerSearchText = _searchText.ToLower();
                query = query.Where(u => u.ExternalId.ToLower().Contains(lowerSearchText) ||
                                         u.Name.ToLower().Contains(lowerSearchText) ||
                                         u.About.ToLower().Contains(lowerSearchText) ||
                                         u.Interests.Any(i => i.ToLower().Equals(lowerSearchText)));
            }

            _totalUsers = await query.CountAsync();
            _totalPages = (int)Math.Ceiling((double)_totalUsers / _pageSize);

            _users = await query
                .OrderByDescending(u => u.UpdatedDate)
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToListAsync();
        }

        private async Task NextPage()
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                await LoadUsers();
                await JSRuntime.InvokeVoidAsync("scrollToTop");
            }
        }

        private async Task PreviousPage()
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                await LoadUsers();
                await JSRuntime.InvokeVoidAsync("scrollToTop");
            }
        }

        private async Task GoToPage(int pageNumber)
        {
            if (pageNumber >= 1 && pageNumber <= _totalPages)
            {
                _currentPage = pageNumber;
                await LoadUsers();
                await JSRuntime.InvokeVoidAsync("scrollToTop");
            }
        }

        private static string ExtractDistanceFromJsonData(JsonElement jsonData)
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

        private static string ExtractShareUrlFromJsonData(JsonElement jsonElement)
        {
            return jsonElement.TryGetProperty("share_url", out var shareUrl) ? shareUrl.GetString() : null;
        }

        private async Task FilterUsers()
        {
            _currentPage = 1; // Сброс на первую страницу при фильтрации
            await LoadUsers();
            await JSRuntime.InvokeVoidAsync("scrollToTop");
        }

        private async Task OnSearch()
        {
            await FilterUsers();
        }

        private async Task OnKeyPress(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                await OnSearch();
            }
        }

        private async Task ClearSearch()
        {
            _searchText = string.Empty;
            await FilterUsers();
        }

        private async Task ShowUserModal(DatingUser user)
        {
            _selectedUser = user;

            await JSRuntime.InvokeVoidAsync("showUserModal");
        }

        private async Task AddToBlacklist(DatingUser user)
        {
            using var dbContext = DbContextFactory.CreateDbContext();

            var isAlreadyBlacklisted = await dbContext.BlacklistedDatingUsers
                .AnyAsync(b => b.Id == user.Id);

            if (!isAlreadyBlacklisted)
            {
                var blacklistedUser = new BlacklistedDatingUser
                {
                    Id = user.Id,
                    CreatedDate = DateTime.UtcNow
                };

                await dbContext.BlacklistedDatingUsers.AddAsync(blacklistedUser);
                await dbContext.SaveChangesAsync();

                var userInList = _users.FirstOrDefault(u => u.Id == user.Id);
                if (userInList != null)
                {
                    userInList.BlacklistedDatingUser = blacklistedUser;
                }

                await InvokeAsync(StateHasChanged);
            }
            else
            {
                throw new InvalidOperationException($"{user.Name} уже находится в черном списке.");
            }
        }
    }
}
