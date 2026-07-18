using LoLClientTool.Models;
using LoLClientTool.Mvc.Services;
using LoLClientTool.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoLClientTool.Controllers
{
    public class RiotIdController : Controller
    {
        private readonly ILeagueClientDetector _leagueClientDetector;
        private readonly ILeagueClientService _leagueClientService;

        public RiotIdController(
            ILeagueClientDetector leagueClientDetector,
            ILeagueClientService leagueClientService)
        {
            _leagueClientDetector = leagueClientDetector;
            _leagueClientService = leagueClientService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            bool isRunning = _leagueClientDetector.IsLeagueClientRunning();

            RiotIdViewModel model = new()
            {
                IsLeagueClientRunning = isRunning,
                ResultMessage = isRunning
                    ? "League Client detected."
                    : "League Client is not running."
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Change(RiotIdViewModel model)
        {
            model.IsLeagueClientRunning = _leagueClientDetector.IsLeagueClientRunning();

            if (!model.IsLeagueClientRunning)
            {
                model.ResultMessage = "League Client is not running.";
                return View("Index", model);
            }

            if (!ModelState.IsValid)
            {
                model.ResultMessage = "Please enter both a Riot ID name and tagline.";
                return View("Index", model);
            }

            LeagueClientResult result = await _leagueClientService.ChangeRiotIdAsync(
                model.GameName ?? string.Empty,
                model.TagLine ?? string.Empty);

            model.ResultMessage = result.Message;
            model.ResponseBody = result.ResponseBody;

            return View("Index", model);
        }
    }
}