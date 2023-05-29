using Microsoft.AspNetCore.Mvc;

namespace VentspilsItc.Controllers
{
    public class RankingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
