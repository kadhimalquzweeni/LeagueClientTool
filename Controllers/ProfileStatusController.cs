using LoLClientTool.Models;
using LoLClientTool.Mvc.Services;
using LoLClientTool.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoLProfileChanger.Mvc.Controllers
{
    public class ProfileStatusController : Controller
    {
        private readonly ILeagueClientDetector _leagueClientDetector;
        private readonly ILeagueClientService _leagueClientService;

        public ProfileStatusController(
            ILeagueClientDetector leagueClientDetector,
            ILeagueClientService leagueClientService)
        {
            _leagueClientDetector = leagueClientDetector;
            _leagueClientService = leagueClientService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            LeagueClientConnection? connection = _leagueClientDetector.GetConnection();

            bool isConnected = connection != null;

            var model = new ProfileStatusViewModel
            {
                IsLeagueClientRunning = isConnected,
                ResultMessage = isConnected
                    ? $"League Client detected on port {connection!.Port}."
                    : "League Client is not running, or the lockfile could not be read. Open League of Legends first, then refresh this page."
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(ProfileStatusViewModel model)
        {
            LeagueClientConnection? connection = _leagueClientDetector.GetConnection();

            bool isLeagueClientRunning = connection != null;

            model.IsLeagueClientRunning = isLeagueClientRunning;

            if (!ModelState.IsValid)
            {
                model.ResultMessage = "Please fix the validation errors.";
                return View(model);
            }

            if (!isLeagueClientRunning)
            {
                model.ResultMessage = "League Client is not running, or the lockfile could not be read. Open League of Legends first, then try again.";
                return View(model);
            }

            LeagueClientResult result =
                await _leagueClientService.UpdateStatusMessageAsync(model.StatusMessageInput!);

            model.ResultMessage = result.Message;

            return View(model);
        }
    }
}