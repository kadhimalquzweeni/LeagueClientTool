using LoLClientTool.Models;
using LoLClientTool.Mvc.Services;
using LoLClientTool.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoLProfileChanger.Mvc.Controllers
{
    public class ChallengeTokensController : Controller
    {
        private readonly ILeagueClientDetector _leagueClientDetector;
        private readonly ILeagueClientService _leagueClientService;

        public ChallengeTokensController(
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

            var model = new ChallengeTokensViewModel
            {
                IsLeagueClientRunning = isConnected,
                ResultMessage = isConnected
                    ? $"League Client detected on port {connection!.Port}."
                    : "League Client is not running, or the lockfile could not be read. Open League of Legends first, then refresh this page."
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Clear()
        {
            LeagueClientConnection? connection = _leagueClientDetector.GetConnection();

            bool isConnected = connection != null;

            var model = new ChallengeTokensViewModel
            {
                IsLeagueClientRunning = isConnected
            };

            if (!isConnected)
            {
                model.ResultMessage = "League Client is not running, or the lockfile could not be read. Open League of Legends first, then try again.";
                return View("Index", model);
            }

            LeagueClientResult result = await _leagueClientService.ClearChallengeTokensAsync();

            model.ResultMessage = result.Message;

            return View("Index", model);
        }
        [HttpPost]
        public async Task<IActionResult> CopyFirstToAll()
        {
            LeagueClientConnection? connection = _leagueClientDetector.GetConnection();

            bool isConnected = connection != null;

            var model = new ChallengeTokensViewModel
            {
                IsLeagueClientRunning = isConnected
            };

            if (!isConnected)
            {
                model.ResultMessage = "League Client is not running, or the lockfile could not be read. Open League of Legends first, then try again.";
                return View("Index", model);
            }

            LeagueClientResult result =
                await _leagueClientService.CopyFirstChallengeTokenToAllSlotsAsync();

            model.ResultMessage = result.Message;

            return View("Index", model);
        }
    }
}