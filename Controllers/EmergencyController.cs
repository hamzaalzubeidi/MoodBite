using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MoodBite.Controllers
{
    [Authorize]
    public class EmergencyController : Controller
    {
        public IActionResult Index() => View();
    }
}
