using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoodBite.Data;
using Microsoft.EntityFrameworkCore;

namespace MoodBite.Controllers
{
    [Authorize]
    public class RestaurantsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public RestaurantsController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? type)
        {
            var query = _db.Restaurants.AsQueryable();
            if (!string.IsNullOrEmpty(type)) query = query.Where(r => r.Type == type);
            var restaurants = await query.ToListAsync();
            ViewBag.SelectedType = type;
            return View(restaurants);
        }
    }
}
