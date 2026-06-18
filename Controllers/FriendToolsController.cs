using LoLClientTool.Models;
using LoLClientTool.Mvc.Services;
using LoLClientTool.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoLProfileChanger.Mvc.Controllers
{
    public class FriendToolsController : Controller
    {
        private readonly ILeagueClientDetector _leagueClientDetector;
        private readonly IFriendToolsService _friendToolsService;

        public FriendToolsController(
            ILeagueClientDetector leagueClientDetector,
            IFriendToolsService friendToolsService)
        {
            _leagueClientDetector = leagueClientDetector;
            _friendToolsService = friendToolsService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(await BuildModelAsync());
        }

        [HttpPost]
        public async Task<IActionResult> AcceptAllRequests()
        {
            LeagueClientResult result =
                await _friendToolsService.AcceptAllFriendRequestsAsync();

            return View("Index", await BuildModelAsync(result.Message));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAllRequests()
        {
            LeagueClientResult result =
                await _friendToolsService.DeleteAllFriendRequestsAsync();

            return View("Index", await BuildModelAsync(result.Message));
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFriendsFromFolder(FriendToolsViewModel model)
        {
            if (!model.SelectedGroupId.HasValue)
            {
                return View("Index", await BuildModelAsync("Please select a friend folder."));
            }

            LeagueClientResult result =
                await _friendToolsService.DeleteFriendsFromGroupAsync(model.SelectedGroupId.Value);

            FriendToolsViewModel updatedModel =
                await BuildModelAsync(result.Message);

            updatedModel.SelectedGroupId = model.SelectedGroupId;

            return View("Index", updatedModel);
        }

        private async Task<FriendToolsViewModel> BuildModelAsync(string? resultMessage = null)
        {
            bool isLeagueClientRunning =
                _leagueClientDetector.IsLeagueClientRunning();

            List<FriendGroupOptionViewModel> friendGroups = isLeagueClientRunning
                ? await _friendToolsService.GetFriendGroupsAsync()
                : new List<FriendGroupOptionViewModel>();

            return new FriendToolsViewModel
            {
                IsLeagueClientRunning = isLeagueClientRunning,
                FriendGroups = friendGroups,
                ResultMessage = resultMessage
                    ?? (isLeagueClientRunning
                        ? "League Client detected."
                        : "League Client is not running. Open League of Legends first, then refresh this page.")
            };
        }
    }
}