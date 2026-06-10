using System.Text.Json;
using MoodBite.Models;

namespace MoodBite.Services
{
    public class MealPlanService
    {
        // Standard algorithm-based meal plan generator
        private static readonly Dictionary<string, List<MealItem>> MealDatabase = new()
        {
            ["keto"] = new()
            {
                new() { NameAr = "بيض مقلي بالزبدة", NameEn = "Butter Fried Eggs", Calories = 350, Protein = 22, Carbs = 2, Fats = 28, Type = "breakfast" },
                new() { NameAr = "سلطة الدجاج بالأفوكادو", NameEn = "Chicken Avocado Salad", Calories = 520, Protein = 42, Carbs = 8, Fats = 35, Type = "lunch" },
                new() { NameAr = "سمك السلمون المشوي", NameEn = "Grilled Salmon", Calories = 480, Protein = 45, Carbs = 3, Fats = 30, Type = "dinner" },
                new() { NameAr = "مكسرات مشكلة", NameEn = "Mixed Nuts", Calories = 180, Protein = 5, Carbs = 6, Fats = 16, Type = "snack" },
                new() { NameAr = "جبن قريش مع الخيار", NameEn = "Cottage Cheese with Cucumber", Calories = 150, Protein = 18, Carbs = 5, Fats = 6, Type = "snack" },
                new() { NameAr = "لحم مفروم مع بروكلي", NameEn = "Ground Beef with Broccoli", Calories = 450, Protein = 40, Carbs = 10, Fats = 28, Type = "dinner" },
                new() { NameAr = "فطيرة البيض بالجبن", NameEn = "Egg and Cheese Omelette", Calories = 380, Protein = 26, Carbs = 3, Fats = 30, Type = "breakfast" },
                new() { NameAr = "دجاج مشوي مع سبانخ", NameEn = "Grilled Chicken with Spinach", Calories = 490, Protein = 50, Carbs = 5, Fats = 28, Type = "lunch" },
            },
            ["mediterranean"] = new()
            {
                new() { NameAr = "خبز العجين المخمر مع زيت الزيتون", NameEn = "Sourdough with Olive Oil", Calories = 320, Protein = 10, Carbs = 48, Fats = 12, Type = "breakfast" },
                new() { NameAr = "شوربة العدس", NameEn = "Lentil Soup", Calories = 380, Protein = 20, Carbs = 55, Fats = 8, Type = "lunch" },
                new() { NameAr = "سمك الدنيس المشوي", NameEn = "Grilled Sea Bass", Calories = 420, Protein = 48, Carbs = 10, Fats = 18, Type = "dinner" },
                new() { NameAr = "حمص مع خضروات", NameEn = "Hummus with Vegetables", Calories = 200, Protein = 8, Carbs = 22, Fats = 10, Type = "snack" },
                new() { NameAr = "زبادي يوناني مع عسل", NameEn = "Greek Yogurt with Honey", Calories = 180, Protein = 12, Carbs = 20, Fats = 5, Type = "snack" },
                new() { NameAr = "دجاج بالليمون والأعشاب", NameEn = "Lemon Herb Chicken", Calories = 450, Protein = 45, Carbs = 12, Fats = 22, Type = "dinner" },
                new() { NameAr = "شكشوكة البيض", NameEn = "Shakshuka", Calories = 340, Protein = 20, Carbs = 25, Fats = 18, Type = "breakfast" },
                new() { NameAr = "سلطة الفتوش", NameEn = "Fattoush Salad", Calories = 280, Protein = 8, Carbs = 38, Fats = 12, Type = "lunch" },
            },
            ["vegan"] = new()
            {
                new() { NameAr = "شوفان مع التوت", NameEn = "Oatmeal with Berries", Calories = 350, Protein = 12, Carbs = 60, Fats = 8, Type = "breakfast" },
                new() { NameAr = "بودة الكينوا", NameEn = "Quinoa Bowl", Calories = 420, Protein = 18, Carbs = 65, Fats = 12, Type = "lunch" },
                new() { NameAr = "كاري الحمص", NameEn = "Chickpea Curry", Calories = 450, Protein = 20, Carbs = 60, Fats = 14, Type = "dinner" },
                new() { NameAr = "فواكه مع بذور القنب", NameEn = "Fruit with Hemp Seeds", Calories = 180, Protein = 6, Carbs = 30, Fats = 6, Type = "snack" },
                new() { NameAr = "لبن الصويا مع موز", NameEn = "Soy Milk with Banana", Calories = 200, Protein = 8, Carbs = 35, Fats = 4, Type = "snack" },
                new() { NameAr = "عدس مع الأرز البني", NameEn = "Lentils with Brown Rice", Calories = 480, Protein = 22, Carbs = 75, Fats = 6, Type = "dinner" },
                new() { NameAr = "تحميصة الأفوكادو", NameEn = "Avocado Toast", Calories = 380, Protein = 10, Carbs = 42, Fats = 20, Type = "breakfast" },
                new() { NameAr = "بورل الفاصولياء السوداء", NameEn = "Black Bean Burrito Bowl", Calories = 500, Protein = 24, Carbs = 72, Fats = 10, Type = "lunch" },
            },
            ["paleo"] = new()
            {
                new() { NameAr = "بيض مع لحم مقدد وسبانخ", NameEn = "Eggs with Bacon and Spinach", Calories = 420, Protein = 30, Carbs = 8, Fats = 30, Type = "breakfast" },
                new() { NameAr = "صدر دجاج مع خضروات مشوية", NameEn = "Chicken Breast with Grilled Vegetables", Calories = 480, Protein = 48, Carbs = 20, Fats = 18, Type = "lunch" },
                new() { NameAr = "ستيك لحم بقري مع بطاطا حلوة", NameEn = "Beef Steak with Sweet Potato", Calories = 550, Protein = 45, Carbs = 35, Fats = 22, Type = "dinner" },
                new() { NameAr = "تفاحة مع لوز", NameEn = "Apple with Almonds", Calories = 200, Protein = 5, Carbs = 25, Fats = 12, Type = "snack" },
                new() { NameAr = "جوز الهند مع توت بري", NameEn = "Coconut with Blueberries", Calories = 160, Protein = 2, Carbs = 18, Fats = 10, Type = "snack" },
                new() { NameAr = "سمك مشوي مع أسباراغوس", NameEn = "Grilled Fish with Asparagus", Calories = 420, Protein = 45, Carbs = 12, Fats = 18, Type = "dinner" },
                new() { NameAr = "وجبة الفطور بالموز والبيض", NameEn = "Banana Egg Pancakes", Calories = 310, Protein = 14, Carbs = 35, Fats = 12, Type = "breakfast" },
                new() { NameAr = "سلطة التونا مع أفوكادو", NameEn = "Tuna Avocado Salad", Calories = 440, Protein = 38, Carbs = 10, Fats = 28, Type = "lunch" },
            },
            ["default"] = new()
            {
                new() { NameAr = "فول مدمس", NameEn = "Foul Medames", Calories = 380, Protein = 20, Carbs = 55, Fats = 8, Type = "breakfast" },
                new() { NameAr = "أرز بالدجاج", NameEn = "Rice with Chicken", Calories = 520, Protein = 42, Carbs = 55, Fats = 12, Type = "lunch" },
                new() { NameAr = "سمك مشوي", NameEn = "Grilled Fish", Calories = 420, Protein = 45, Carbs = 8, Fats = 18, Type = "dinner" },
                new() { NameAr = "فاكهة طازجة", NameEn = "Fresh Fruit", Calories = 120, Protein = 2, Carbs = 28, Fats = 1, Type = "snack" },
                new() { NameAr = "مكسرات وتمر", NameEn = "Nuts and Dates", Calories = 200, Protein = 5, Carbs = 28, Fats = 10, Type = "snack" },
                new() { NameAr = "شوربة خضار", NameEn = "Vegetable Soup", Calories = 180, Protein = 8, Carbs = 25, Fats = 5, Type = "dinner" },
                new() { NameAr = "بيض مسلوق مع خبز", NameEn = "Boiled Eggs with Bread", Calories = 320, Protein = 18, Carbs = 35, Fats = 10, Type = "breakfast" },
                new() { NameAr = "سلطة خضراء", NameEn = "Green Salad", Calories = 150, Protein = 5, Carbs = 18, Fats = 6, Type = "lunch" },
            }
        };

        private static readonly string[] DayNamesAr = ["الاثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت", "الأحد"];
        private static readonly string[] DayNamesEn = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

        public string GenerateWeekPlan(string dietSlug, int seed = 0)
        {
            var rng = new Random(seed == 0 ? DateTime.Now.Millisecond : seed);
            var meals = MealDatabase.ContainsKey(dietSlug) ? MealDatabase[dietSlug] : MealDatabase["default"];

            var days = new List<object>();
            for (int d = 0; d < 7; d++)
            {
                var breakfasts = meals.Where(m => m.Type == "breakfast").OrderBy(_ => rng.Next()).ToList();
                var lunches = meals.Where(m => m.Type == "lunch").OrderBy(_ => rng.Next()).ToList();
                var dinners = meals.Where(m => m.Type == "dinner").OrderBy(_ => rng.Next()).ToList();
                var snacks = meals.Where(m => m.Type == "snack").OrderBy(_ => rng.Next()).ToList();

                if (!breakfasts.Any()) breakfasts = meals.Take(1).ToList();
                if (!lunches.Any()) lunches = meals.Skip(1).Take(1).ToList();
                if (!dinners.Any()) dinners = meals.Skip(2).Take(1).ToList();
                if (snacks.Count < 2) snacks = meals.Skip(3).Take(2).ToList();

                var breakfast = breakfasts.First();
                var lunch = lunches.First();
                var dinner = dinners.First();
                var snack1 = snacks.Count > 0 ? snacks[0] : meals.Last();
                var snack2 = snacks.Count > 1 ? snacks[1] : meals.Last();

                days.Add(new
                {
                    day = d + 1,
                    dayNameAr = DayNamesAr[d],
                    dayNameEn = DayNamesEn[d],
                    totalCalories = breakfast.Calories + lunch.Calories + dinner.Calories + snack1.Calories + snack2.Calories,
                    totalProtein = breakfast.Protein + lunch.Protein + dinner.Protein + snack1.Protein + snack2.Protein,
                    totalCarbs = breakfast.Carbs + lunch.Carbs + dinner.Carbs + snack1.Carbs + snack2.Carbs,
                    totalFats = breakfast.Fats + lunch.Fats + dinner.Fats + snack1.Fats + snack2.Fats,
                    meals = new[]
                    {
                        new { type = "breakfast", nameAr = breakfast.NameAr, nameEn = breakfast.NameEn, calories = breakfast.Calories, protein = breakfast.Protein, carbs = breakfast.Carbs, fats = breakfast.Fats },
                        new { type = "snack1", nameAr = snack1.NameAr, nameEn = snack1.NameEn, calories = snack1.Calories, protein = snack1.Protein, carbs = snack1.Carbs, fats = snack1.Fats },
                        new { type = "lunch", nameAr = lunch.NameAr, nameEn = lunch.NameEn, calories = lunch.Calories, protein = lunch.Protein, carbs = lunch.Carbs, fats = lunch.Fats },
                        new { type = "snack2", nameAr = snack2.NameAr, nameEn = snack2.NameEn, calories = snack2.Calories, protein = snack2.Protein, carbs = snack2.Carbs, fats = snack2.Fats },
                        new { type = "dinner", nameAr = dinner.NameAr, nameEn = dinner.NameEn, calories = dinner.Calories, protein = dinner.Protein, carbs = dinner.Carbs, fats = dinner.Fats },
                    }
                });
            }

            return JsonSerializer.Serialize(new { days });
        }

        public string GenerateShoppingList(string planJson)
        {
            // Return categorized shopping list from meal plan
            var list = new
            {
                proteins = new[] { "دجاج", "بيض", "سمك", "لحم بقري" },
                vegetables = new[] { "طماطم", "خيار", "سبانخ", "بروكلي", "خضروات مشكلة" },
                grains = new[] { "خبز أسمر", "أرز بني", "شوفان", "كينوا" },
                dairy = new[] { "زبادي يوناني", "جبن", "لبن" },
                supplements = new[] { "زيت زيتون", "مكسرات مشكلة", "بذور شيا" }
            };
            return JsonSerializer.Serialize(list);
        }

        private class MealItem
        {
            public string NameAr { get; set; } = string.Empty;
            public string NameEn { get; set; } = string.Empty;
            public int Calories { get; set; }
            public int Protein { get; set; }
            public int Carbs { get; set; }
            public int Fats { get; set; }
            public string Type { get; set; } = string.Empty;
        }
    }
}
