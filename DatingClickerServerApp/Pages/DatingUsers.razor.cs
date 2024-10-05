using DatingClickerServerApp.Common.Extensions;
using DatingClickerServerApp.Common.Model;
using DatingClickerServerApp.Common.Persistence;
using DatingClickerServerApp.Common.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using System.Text.Json;

namespace DatingClickerServerApp.Pages
{
    public partial class DatingUsers : ComponentBase
    {
        private List<DatingUser> _users;
        private bool _onlyVerified = false;
        private bool _onlyToday = true;
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
            IQueryable<DatingUser> query = dbContext.DatingUsers.Include(u => u.Actions);

            if (_onlyVerified)
            {
                query = query.Where(u => u.IsVerified);
            }

            if (_onlyToday)
            {
                var startOfToday = DateTime.UtcNow.GetStartOfDay();
                query = query.Where(u => u.Actions.Any(a => a.CreatedDate >= startOfToday));
            }

            if (!string.IsNullOrEmpty(_searchText))
            {
                var lowerSearchText = _searchText.ToLower();
                query = query.Where(u => u.ExternalId.ToLower().Contains(lowerSearchText) ||
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

            var modal = await JSRuntime.InvokeAsync<IJSObjectReference>("bootstrap.Modal.getOrCreateInstance", "#userModal");
            await modal.InvokeVoidAsync("show");
        }
    }
}
