using DatingClickerServerApp.Common.Model;
using DatingClickerServerApp.Core.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using DatingClickerServerApp.UI.Components.Model;

namespace DatingClickerServerApp.UI.Pages
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
        private bool _onlyToday = false;
        private ICollection<DatingUserActionType> _selectedAllActionTypes = [];
        private ICollection<DatingUserActionType> _selectedLastActionTypes = [];
        private string _searchText = string.Empty;

        private int _currentPage = 1;
        private int _pageSize = 100;
        private int _totalUsers;
        private int _totalPages;

        private DatingUser _selectedUser;

        [Inject] private IDatingUserService DatingUserService { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadUsers();
        }

        private async Task LoadUsers()
        {
            _users = await DatingUserService.GetUsers(
                _onlyVerified,
                _onlyToday,
                _selectedAllActionTypes,
                _selectedLastActionTypes,
                _searchText,
                _currentPage,
                _pageSize
            );

            _totalUsers = await DatingUserService.GetTotalUsersCount(
                _onlyVerified,
                _onlyToday,
                _selectedAllActionTypes,
                _selectedLastActionTypes,
                _searchText
            );

            _totalPages = (int)Math.Ceiling((double)_totalUsers / _pageSize);
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

        private async Task FilterUsers()
        {
            _currentPage = 1;
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
            await DatingUserService.AddToBlacklist(user);

            var userInList = _users.FirstOrDefault(u => u.Id == user.Id);
            if (userInList != null)
            {
                userInList.BlacklistedDatingUser = new BlacklistedDatingUser { Id = user.Id, CreatedDate = DateTime.UtcNow };
            }

            await InvokeAsync(StateHasChanged);
        }
    }
}
