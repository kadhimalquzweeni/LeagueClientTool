using LoLClientTool.Models;
using LoLClientTool.Mvc.Services;
using LoLClientTool.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoLProfileChanger.Mvc.Controllers
{
    public class CustomRequestController : Controller
    {
        private readonly ILeagueClientDetector _leagueClientDetector;
        private readonly ILeagueClientService _leagueClientService;

        public CustomRequestController(
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

            var model = new CustomRequestViewModel
            {
                IsLeagueClientRunning = isConnected,
                ResultMessage = isConnected
                    ? $"League Client detected on port {connection!.Port}."
                    : "League Client is not running, or the lockfile could not be read. Open League of Legends first, then refresh this page.",
                Method = "GET",
                Endpoint = ""
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(CustomRequestViewModel model)
        {
            LeagueClientConnection? connection = _leagueClientDetector.GetConnection();

            bool isConnected = connection != null;

            model.IsLeagueClientRunning = isConnected;

            if (!isConnected)
            {
                model.ResultMessage = "League Client is not running, or the lockfile could not be read. Open League of Legends first, then try again.";
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                model.ResultMessage = "Please fix the validation errors.";
                return View(model);
            }

            CustomLeagueClientRequestResult result =
                await _leagueClientService.SendCustomRequestAsync(
                    model.Method,
                    model.Endpoint,
                    model.Body);

            model.RequestSucceeded = result.Success;
            model.StatusCode = result.StatusCode;
            model.ResponseBody = result.ResponseBody;
            model.ResultMessage = result.Message;

            return View(model);
        }
    }
}