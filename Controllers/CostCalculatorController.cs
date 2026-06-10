using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoodBite.Data;
using Microsoft.EntityFrameworkCore;

namespace MoodBite.Controllers
{
    [Authorize]
    public class CostCalculatorController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CostCalculatorController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var diets = await _db.Diets.Where(d => d.IsActive).ToListAsync();
            ViewBag.Diets = diets;

            // Cost data per diet per country (in local currency)
            var costData = GetCostData();
            ViewBag.CostData = costData;

            return View();
        }

        private static Dictionary<string, Dictionary<string, double>> GetCostData()
        {
            // Base costs in JOD, multiplied by country factor
            var baseCosts = new Dictionary<string, double>
            {
                ["keto"] = 150, ["mediterranean"] = 110, ["vegan"] = 80,
                ["paleo"] = 140, ["dash"] = 100, ["intermittent-fasting"] = 90,
                ["flexitarian"] = 85, ["carnivore"] = 160
            };

            var countryMultipliers = new Dictionary<string, double>
            {
                ["jordan"] = 1.0, ["saudi"] = 3.75, ["uae"] = 3.67, ["egypt"] = 30.8, ["kuwait"] = 0.31
            };

            var currencies = new Dictionary<string, string>
            {
                ["jordan"] = "JOD", ["saudi"] = "SAR", ["uae"] = "AED", ["egypt"] = "EGP", ["kuwait"] = "KWD"
            };

            var result = new Dictionary<string, Dictionary<string, double>>();
            foreach (var diet in baseCosts)
            {
                result[diet.Key] = new Dictionary<string, double>();
                foreach (var country in countryMultipliers)
                    result[diet.Key][country.Key] = Math.Round(diet.Value * country.Value, 2);
            }
            return result;
        }
    }
}
