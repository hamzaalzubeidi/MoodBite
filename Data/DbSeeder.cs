using Microsoft.AspNetCore.Identity;
using MoodBite.Constants;
using MoodBite.Models;
using MoodBite.Services;
using System.Text.Json;

namespace MoodBite.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services, bool seedDemoData = false)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var db = services.GetRequiredService<ApplicationDbContext>();

            // Ensure database is created
            db.Database.EnsureCreated();

            // Seed roles
            foreach (var role in ApplicationRoles.SeededRoles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Seed demo/default admin only when explicitly enabled for local prototype data.
            if (seedDemoData && await userManager.FindByEmailAsync("admin@moodbite.com") == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin@moodbite.com",
                    Email = "admin@moodbite.com",
                    FullName = "مدير النظام",
                    IsActive = true,
                    EmailConfirmed = true,
                    PreferredLanguage = "ar"
                };
                var result = await userManager.CreateAsync(admin, "Admin@123456");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, ApplicationRoles.Admin);
            }

            // Seed diets
            if (!db.Diets.Any())
            {
                var diets = new List<Diet>
                {
                    new()
                    {
                        Slug = "keto",
                        NameAr = "الكيتو",
                        NameEn = "Keto",
                        DescriptionAr = "نظام غذائي عالي الدهون، منخفض الكربوهيدرات يحوّل الجسم إلى حرق الدهون للحصول على الطاقة",
                        DescriptionEn = "A high-fat, low-carb diet that shifts your body into burning fat for fuel",
                        Category = "low-carb",
                        Difficulty = "hard",
                        ColorGradient = "linear-gradient(135deg, #f59e0b, #d97706)",
                        Emoji = "🥑",
                        CalorieMin = 1500,
                        CalorieMax = 2500,
                        BenefitsJson = """{"ar":["فقدان الوزن السريع","تحسين مستوى الطاقة","تقليل الشهية","تحسين نسبة السكر في الدم","تحسين الوضوح الذهني"],"en":["Rapid weight loss","Improved energy levels","Reduced appetite","Better blood sugar control","Enhanced mental clarity"]}""",
                        FoodsJson = """[{"name":"Beef","nameAr":"لحم بقري","status":"allowed"},{"name":"Fish","nameAr":"سمك","status":"allowed"},{"name":"Eggs","nameAr":"بيض","status":"allowed"},{"name":"Cheese","nameAr":"جبن","status":"allowed"},{"name":"Avocado","nameAr":"أفوكادو","status":"allowed"},{"name":"Spinach","nameAr":"سبانخ","status":"allowed"},{"name":"Broccoli","nameAr":"بروكلي","status":"allowed"},{"name":"Olive Oil","nameAr":"زيت زيتون","status":"allowed"},{"name":"Nuts","nameAr":"مكسرات","status":"limited"},{"name":"Berries","nameAr":"توت","status":"limited"},{"name":"Bread","nameAr":"خبز","status":"forbidden"},{"name":"Rice","nameAr":"أرز","status":"forbidden"},{"name":"Sugar","nameAr":"سكر","status":"forbidden"},{"name":"Pasta","nameAr":"معكرونة","status":"forbidden"},{"name":"Potatoes","nameAr":"بطاطا","status":"forbidden"}]""",
                        SampleMealsJson = """[{"nameAr":"بيض مع بيكون وأفوكادو","nameEn":"Eggs with Bacon and Avocado","calories":450,"protein":28,"carbs":5,"fats":35},{"nameAr":"سلطة دجاج بالجبن","nameEn":"Chicken Cheese Salad","calories":520,"protein":45,"carbs":8,"fats":32},{"nameAr":"سمك السلمون المشوي مع بروكلي","nameEn":"Grilled Salmon with Broccoli","calories":480,"protein":42,"carbs":10,"fats":28}]""",
                        TipsJson = """{"ar":["ابدأ تدريجياً لتجنب الإنفلونزا الكيتونية","اشرب الكثير من الماء","احرص على الأملاح المعدنية","راقب نسبة الكربوهيدرات بدقة","استشر طبيبك إذا كان لديك مرض سكري"],"en":["Start gradually to avoid keto flu","Drink plenty of water","Maintain electrolyte balance","Track carbs carefully","Consult doctor if you have diabetes"]}"""
                    },
                    new()
                    {
                        Slug = "mediterranean",
                        NameAr = "البحر المتوسط",
                        NameEn = "Mediterranean",
                        DescriptionAr = "نظام غذائي متوازن مستوحى من شعوب حوض البحر المتوسط، غني بالخضار والزيوت الصحية",
                        DescriptionEn = "A balanced diet inspired by Mediterranean cultures, rich in vegetables and healthy oils",
                        Category = "balanced",
                        Difficulty = "easy",
                        ColorGradient = "linear-gradient(135deg, #3b82f6, #0891b2)",
                        Emoji = "🫒",
                        CalorieMin = 1800,
                        CalorieMax = 2800,
                        BenefitsJson = """{"ar":["صحة القلب والأوعية الدموية","تقليل الالتهابات","الوقاية من الأمراض المزمنة","تحسين صحة الدماغ","تعزيز طول العمر"],"en":["Heart health","Reduced inflammation","Chronic disease prevention","Better brain health","Longevity"]}""",
                        FoodsJson = """[{"name":"Olive Oil","nameAr":"زيت زيتون","status":"allowed"},{"name":"Fish","nameAr":"سمك","status":"allowed"},{"name":"Vegetables","nameAr":"خضروات","status":"allowed"},{"name":"Legumes","nameAr":"بقوليات","status":"allowed"},{"name":"Whole Grains","nameAr":"حبوب كاملة","status":"allowed"},{"name":"Nuts","nameAr":"مكسرات","status":"allowed"},{"name":"Poultry","nameAr":"دواجن","status":"limited"},{"name":"Eggs","nameAr":"بيض","status":"limited"},{"name":"Red Meat","nameAr":"لحم أحمر","status":"limited"},{"name":"Processed Food","nameAr":"طعام مصنع","status":"forbidden"},{"name":"Added Sugar","nameAr":"سكر مضاف","status":"forbidden"}]""",
                        SampleMealsJson = """[{"nameAr":"شكشوكة البيض","nameEn":"Shakshuka","calories":340,"protein":20,"carbs":25,"fats":18},{"nameAr":"سلطة الفتوش مع حمص","nameEn":"Fattoush Salad with Hummus","calories":380,"protein":14,"carbs":48,"fats":16},{"nameAr":"سمك الدنيس المشوي مع خضار","nameEn":"Grilled Sea Bass with Vegetables","calories":420,"protein":45,"carbs":18,"fats":20}]""",
                        TipsJson = """{"ar":["استخدم زيت الزيتون البكر كمصدر رئيسي للدهون","تناول الأسماك مرتين أسبوعياً على الأقل","أكثر من الخضار والبقوليات","قلل من اللحوم الحمراء","استمتع بالوجبات مع العائلة"],"en":["Use extra virgin olive oil as main fat source","Eat fish at least twice a week","Load up on vegetables and legumes","Limit red meat","Enjoy meals with family"]}"""
                    },
                    new()
                    {
                        Slug = "vegan",
                        NameAr = "النظام النباتي الكامل",
                        NameEn = "Vegan",
                        DescriptionAr = "نظام غذائي يعتمد كلياً على النباتات، يستبعد جميع المنتجات الحيوانية",
                        DescriptionEn = "A fully plant-based diet that excludes all animal products",
                        Category = "plant-based",
                        Difficulty = "medium",
                        ColorGradient = "linear-gradient(135deg, #22c55e, #16a34a)",
                        Emoji = "🌱",
                        CalorieMin = 1600,
                        CalorieMax = 2400,
                        BenefitsJson = """{"ar":["خفض الوزن","تحسين صحة القلب","تقليل خطر السرطان","صديق للبيئة","تحسين الهضم"],"en":["Weight loss","Better heart health","Reduced cancer risk","Eco-friendly","Improved digestion"]}""",
                        FoodsJson = """[{"name":"Legumes","nameAr":"بقوليات","status":"allowed"},{"name":"Vegetables","nameAr":"خضروات","status":"allowed"},{"name":"Fruits","nameAr":"فواكه","status":"allowed"},{"name":"Whole Grains","nameAr":"حبوب كاملة","status":"allowed"},{"name":"Nuts & Seeds","nameAr":"مكسرات وبذور","status":"allowed"},{"name":"Plant Oils","nameAr":"زيوت نباتية","status":"allowed"},{"name":"Meat","nameAr":"لحوم","status":"forbidden"},{"name":"Dairy","nameAr":"منتجات الألبان","status":"forbidden"},{"name":"Eggs","nameAr":"بيض","status":"forbidden"},{"name":"Honey","nameAr":"عسل","status":"forbidden"}]""",
                        SampleMealsJson = """[{"nameAr":"شوفان مع التوت وبذور الشيا","nameEn":"Oatmeal with Berries and Chia Seeds","calories":380,"protein":12,"carbs":65,"fats":10},{"nameAr":"بودة الكينوا","nameEn":"Quinoa Power Bowl","calories":450,"protein":20,"carbs":68,"fats":14},{"nameAr":"كاري الحمص","nameEn":"Chickpea Curry","calories":460,"protein":22,"carbs":62,"fats":14}]""",
                        TipsJson = """{"ar":["احرص على الحصول على فيتامين B12","تناول مصادر متنوعة من البروتين النباتي","اهتم بمستوى الحديد والزنك","اشرب حليب النباتات المدعومة","استشر اختصاصي تغذية"],"en":["Ensure adequate vitamin B12","Vary your plant protein sources","Monitor iron and zinc levels","Drink fortified plant milks","Consult a nutritionist"]}"""
                    },
                    new()
                    {
                        Slug = "paleo",
                        NameAr = "الباليو",
                        NameEn = "Paleo",
                        DescriptionAr = "نظام غذائي مستوحى من عصر ما قبل التاريخ، يركز على الأطعمة الطبيعية غير المصنعة",
                        DescriptionEn = "A diet inspired by prehistoric times, focusing on whole, unprocessed foods",
                        Category = "high-protein",
                        Difficulty = "medium",
                        ColorGradient = "linear-gradient(135deg, #92400e, #b45309)",
                        Emoji = "🥩",
                        CalorieMin = 1700,
                        CalorieMax = 2700,
                        BenefitsJson = """{"ar":["فقدان الوزن","تحسين التحكم في سكر الدم","تقليل الالتهابات","تحسين الهضم","زيادة الطاقة"],"en":["Weight loss","Better blood sugar control","Reduced inflammation","Improved digestion","Increased energy"]}""",
                        FoodsJson = """[{"name":"Lean Meats","nameAr":"لحوم خالية من الدهون","status":"allowed"},{"name":"Fish","nameAr":"سمك","status":"allowed"},{"name":"Eggs","nameAr":"بيض","status":"allowed"},{"name":"Vegetables","nameAr":"خضروات","status":"allowed"},{"name":"Fruits","nameAr":"فواكه","status":"allowed"},{"name":"Nuts","nameAr":"مكسرات","status":"allowed"},{"name":"Sweet Potatoes","nameAr":"بطاطا حلوة","status":"allowed"},{"name":"Grains","nameAr":"حبوب","status":"forbidden"},{"name":"Legumes","nameAr":"بقوليات","status":"forbidden"},{"name":"Dairy","nameAr":"ألبان","status":"forbidden"},{"name":"Processed Food","nameAr":"طعام مصنع","status":"forbidden"}]""",
                        SampleMealsJson = """[{"nameAr":"بيض مقلي مع سبانخ ولحم مقدد","nameEn":"Scrambled Eggs with Spinach and Bacon","calories":420,"protein":30,"carbs":8,"fats":30},{"nameAr":"صدر دجاج مع خضروات مشوية","nameEn":"Grilled Chicken Breast with Roasted Vegetables","calories":490,"protein":50,"carbs":22,"fats":18},{"nameAr":"ستيك لحم بقري مع بطاطا حلوة","nameEn":"Beef Steak with Sweet Potato","calories":560,"protein":48,"carbs":38,"fats":22}]""",
                        TipsJson = """{"ar":["تجنب الحبوب والبقوليات تماماً","اختر اللحوم العضوية إن أمكن","تناول الخضروات الجذرية باعتدال","اطبخ بزيت جوز الهند أو زيت الزيتون","تجنب السكر بجميع أشكاله"],"en":["Avoid all grains and legumes","Choose organic meats when possible","Eat root vegetables in moderation","Cook with coconut or olive oil","Avoid sugar in all forms"]}"""
                    },
                    new()
                    {
                        Slug = "dash",
                        NameAr = "داش",
                        NameEn = "DASH",
                        DescriptionAr = "نظام غذائي لعلاج ارتفاع ضغط الدم، يركز على تقليل الصوديوم وزيادة البوتاسيوم",
                        DescriptionEn = "Dietary Approaches to Stop Hypertension - reduces sodium and increases potassium",
                        Category = "balanced",
                        Difficulty = "easy",
                        ColorGradient = "linear-gradient(135deg, #ef4444, #dc2626)",
                        Emoji = "❤️",
                        CalorieMin = 1800,
                        CalorieMax = 2600,
                        BenefitsJson = """{"ar":["خفض ضغط الدم","تحسين صحة القلب","تقليل خطر أمراض القلب","تحسين مستوى الكوليسترول","تقليل خطر السكتة الدماغية"],"en":["Lower blood pressure","Better heart health","Reduced heart disease risk","Improved cholesterol levels","Lower stroke risk"]}""",
                        FoodsJson = """[{"name":"Vegetables","nameAr":"خضروات","status":"allowed"},{"name":"Fruits","nameAr":"فواكه","status":"allowed"},{"name":"Whole Grains","nameAr":"حبوب كاملة","status":"allowed"},{"name":"Low-fat Dairy","nameAr":"ألبان قليلة الدهون","status":"allowed"},{"name":"Lean Protein","nameAr":"بروتين خالٍ من الدهون","status":"allowed"},{"name":"Nuts","nameAr":"مكسرات","status":"limited"},{"name":"Salt","nameAr":"ملح","status":"limited"},{"name":"Red Meat","nameAr":"لحم أحمر","status":"limited"},{"name":"Processed Food","nameAr":"طعام مصنع","status":"forbidden"},{"name":"Sugary Drinks","nameAr":"مشروبات سكرية","status":"forbidden"}]""",
                        SampleMealsJson = """[{"nameAr":"شوفان مع موز وتوت","nameEn":"Oatmeal with Banana and Berries","calories":360,"protein":12,"carbs":68,"fats":6},{"nameAr":"شوربة خضار مع خبز أسمر","nameEn":"Vegetable Soup with Whole Wheat Bread","calories":380,"protein":16,"carbs":58,"fats":8},{"nameAr":"دجاج مشوي مع أرز بني وسلطة","nameEn":"Grilled Chicken with Brown Rice and Salad","calories":500,"protein":45,"carbs":50,"fats":12}]""",
                        TipsJson = """{"ar":["قلل الملح وجرب الأعشاب والتوابل","تناول 4-5 حصص من الخضار يومياً","اختر منتجات الألبان قليلة الدهون","تجنب الأطعمة المصنعة والمعلبة","اقرأ ملصقات الغذاء للتحقق من نسبة الصوديوم"],"en":["Reduce salt and try herbs and spices","Eat 4-5 servings of vegetables daily","Choose low-fat dairy products","Avoid processed and canned foods","Read food labels to check sodium content"]}"""
                    },
                    new()
                    {
                        Slug = "intermittent-fasting",
                        NameAr = "الصيام المتقطع",
                        NameEn = "Intermittent Fasting",
                        DescriptionAr = "نمط أكل يتناوب بين فترات الصيام والأكل، الأكثر شيوعاً هو 16:8",
                        DescriptionEn = "An eating pattern that alternates between fasting and eating periods, most popular is 16:8",
                        Category = "fasting",
                        Difficulty = "medium",
                        ColorGradient = "linear-gradient(135deg, #8b5cf6, #7c3aed)",
                        Emoji = "⏰",
                        CalorieMin = 1600,
                        CalorieMax = 2400,
                        BenefitsJson = """{"ar":["فقدان الوزن","تحسين حساسية الأنسولين","تنظيف الخلايا (الأوتوفاجي)","تحسين صحة الدماغ","إطالة العمر"],"en":["Weight loss","Improved insulin sensitivity","Cellular cleanup (autophagy)","Better brain health","Longevity"]}""",
                        FoodsJson = """[{"name":"All Whole Foods","nameAr":"جميع الأطعمة الكاملة","status":"allowed"},{"name":"Water","nameAr":"ماء","status":"allowed"},{"name":"Coffee (black)","nameAr":"قهوة سوداء","status":"allowed"},{"name":"Tea","nameAr":"شاي","status":"allowed"},{"name":"Processed Snacks","nameAr":"وجبات خفيفة مصنعة","status":"forbidden"},{"name":"Sugary Drinks","nameAr":"مشروبات سكرية","status":"forbidden"},{"name":"Alcohol","nameAr":"كحول","status":"forbidden"}]""",
                        SampleMealsJson = """[{"nameAr":"فطور الظهر: بيض مع أفوكادو وخبز","nameEn":"Brunch: Eggs with Avocado and Bread","calories":480,"protein":28,"carbs":32,"fats":28},{"nameAr":"غداء متأخر: دجاج مع أرز وخضار","nameEn":"Late Lunch: Chicken Rice and Vegetables","calories":560,"protein":48,"carbs":55,"fats":15},{"nameAr":"عشاء: سلمون مع سلطة كبيرة","nameEn":"Dinner: Salmon with Large Salad","calories":520,"protein":48,"carbs":18,"fats":28}]""",
                        TipsJson = """{"ar":["ابدأ بنافذة أكل 10 ساعات وقللها تدريجياً","اشرب الكثير من الماء خلال الصيام","كسر الصيام بوجبة متوازنة","لا تعوض الصيام بالإفراط في الأكل","تجنب الصيام إذا كنت حاملاً أو مريضاً بالسكري"],"en":["Start with 10-hour eating window and reduce gradually","Drink plenty of water while fasting","Break fast with a balanced meal","Don't overeat to compensate for fasting","Avoid fasting if pregnant or diabetic"]}"""
                    },
                    new()
                    {
                        Slug = "flexitarian",
                        NameAr = "المرن النباتي",
                        NameEn = "Flexitarian",
                        DescriptionAr = "نظام غذائي يركز على النباتات مع السماح بكميات صغيرة من اللحوم",
                        DescriptionEn = "A mostly plant-based diet that allows small amounts of meat",
                        Category = "plant-based",
                        Difficulty = "easy",
                        ColorGradient = "linear-gradient(135deg, #10b981, #059669)",
                        Emoji = "🥗",
                        CalorieMin = 1700,
                        CalorieMax = 2600,
                        BenefitsJson = """{"ar":["فقدان الوزن","تحسين صحة القلب","مرونة في الخيارات الغذائية","صديق للبيئة","تقليل خطر الأمراض المزمنة"],"en":["Weight management","Heart health","Dietary flexibility","Eco-friendly","Reduced chronic disease risk"]}""",
                        FoodsJson = """[{"name":"Vegetables","nameAr":"خضروات","status":"allowed"},{"name":"Fruits","nameAr":"فواكه","status":"allowed"},{"name":"Legumes","nameAr":"بقوليات","status":"allowed"},{"name":"Whole Grains","nameAr":"حبوب كاملة","status":"allowed"},{"name":"Eggs","nameAr":"بيض","status":"allowed"},{"name":"Dairy","nameAr":"ألبان","status":"allowed"},{"name":"Fish","nameAr":"سمك","status":"allowed"},{"name":"Poultry","nameAr":"دواجن","status":"limited"},{"name":"Red Meat","nameAr":"لحم أحمر","status":"limited"},{"name":"Processed Meat","nameAr":"لحوم مصنعة","status":"forbidden"}]""",
                        SampleMealsJson = """[{"nameAr":"بودة الإفطار النباتية","nameEn":"Vegan Breakfast Bowl","calories":380,"protein":14,"carbs":62,"fats":10},{"nameAr":"حساء العدس مع الخبز","nameEn":"Lentil Soup with Bread","calories":420,"protein":22,"carbs":62,"fats":8},{"nameAr":"دجاج مشوي مع خضار كثيرة","nameEn":"Grilled Chicken with Lots of Vegetables","calories":480,"protein":45,"carbs":28,"fats":18}]""",
                        TipsJson = """{"ar":["ابدأ بيوم نباتي كامل في الأسبوع","زد كميات البقوليات والبذور","جرب وصفات نباتية جديدة","قلل تدريجياً من اللحوم","استمتع بالتنوع الغذائي"],"en":["Start with one fully plant-based day per week","Increase legumes and seeds","Try new vegetarian recipes","Gradually reduce meat portions","Enjoy dietary variety"]}"""
                    },
                    new()
                    {
                        Slug = "carnivore",
                        NameAr = "آكل اللحوم",
                        NameEn = "Carnivore",
                        DescriptionAr = "نظام غذائي يعتمد حصرياً على اللحوم والمنتجات الحيوانية",
                        DescriptionEn = "A diet that exclusively relies on meat and animal products",
                        Category = "high-protein",
                        Difficulty = "hard",
                        ColorGradient = "linear-gradient(135deg, #dc2626, #991b1b)",
                        Emoji = "🥩",
                        CalorieMin = 1800,
                        CalorieMax = 3000,
                        BenefitsJson = """{"ar":["فقدان الوزن السريع","تبسيط اختيار الطعام","تحسين بناء العضلات","تقليل الالتهابات","تحسين الهضم لبعض الأشخاص"],"en":["Rapid weight loss","Simplified food choices","Improved muscle building","Reduced inflammation","Digestive improvement for some"]}""",
                        FoodsJson = """[{"name":"Beef","nameAr":"لحم بقري","status":"allowed"},{"name":"Lamb","nameAr":"لحم غنم","status":"allowed"},{"name":"Pork","nameAr":"لحم خنزير","status":"allowed"},{"name":"Poultry","nameAr":"دواجن","status":"allowed"},{"name":"Fish","nameAr":"سمك","status":"allowed"},{"name":"Eggs","nameAr":"بيض","status":"allowed"},{"name":"Butter","nameAr":"زبدة","status":"allowed"},{"name":"Cheese","nameAr":"جبن","status":"limited"},{"name":"Vegetables","nameAr":"خضروات","status":"forbidden"},{"name":"Fruits","nameAr":"فواكه","status":"forbidden"},{"name":"Grains","nameAr":"حبوب","status":"forbidden"},{"name":"Sugar","nameAr":"سكر","status":"forbidden"}]""",
                        SampleMealsJson = """[{"nameAr":"ستيك ريبي مع بيض","nameEn":"Ribeye Steak with Eggs","calories":650,"protein":55,"carbs":0,"fats":45},{"nameAr":"برجر لحم بدون خبز","nameEn":"Bunless Beef Burger","calories":520,"protein":45,"carbs":0,"fats":38},{"nameAr":"سمك السلمون مع زبدة","nameEn":"Salmon with Butter","calories":480,"protein":42,"carbs":0,"fats":33}]""",
                        TipsJson = """{"ar":["استشر طبيبك قبل البدء","تتبع مؤشرات الدم بانتظام","ابدأ بلحوم الأعضاء للحصول على المغذيات","اختر اللحوم من مصادر جيدة","انتبه لصحة الكلى"],"en":["Consult your doctor before starting","Monitor blood markers regularly","Include organ meats for nutrients","Source quality meats","Monitor kidney health"]}"""
                    }
                };

                await db.Diets.AddRangeAsync(diets);
                await db.SaveChangesAsync();
            }

            // Seed challenges
            if (!db.Challenges.Any())
            {
                var challenges = new List<Challenge>
                {
                    new()
                    {
                        Slug = "keto-challenge",
                        NameAr = "تحدي الكيتو 30 يوم",
                        NameEn = "30-Day Keto Challenge",
                        DescriptionAr = "اتبع نظام الكيتو لمدة 30 يوماً وحوّل جسمك",
                        DescriptionEn = "Follow the keto diet for 30 days and transform your body",
                        Difficulty = "hard",
                        DietType = "keto",
                        Emoji = "🥑",
                        TasksJson = """[{"day":1,"taskAr":"قلل الكربوهيدرات إلى أقل من 20 غرام","taskEn":"Reduce carbs to under 20g"},{"day":2,"taskAr":"اشرب 3 لتر ماء","taskEn":"Drink 3 liters of water"},{"day":3,"taskAr":"تناول وجبة غنية بالدهون الصحية","taskEn":"Eat a meal rich in healthy fats"},{"day":4,"taskAr":"قم بتمرين المشي 30 دقيقة","taskEn":"Walk for 30 minutes"},{"day":5,"taskAr":"جهّز وجبتك الكيتو مسبقاً","taskEn":"Meal prep your keto meals"},{"day":6,"taskAr":"اختبر مستوى الكيتونات إن أمكن","taskEn":"Test ketone levels if possible"},{"day":7,"taskAr":"يوم راحة واسترخاء","taskEn":"Rest and recovery day"}]"""
                    },
                    new()
                    {
                        Slug = "mediterranean-challenge",
                        NameAr = "تحدي البحر المتوسط",
                        NameEn = "Mediterranean Challenge",
                        DescriptionAr = "تبنّ أسلوب حياة البحر المتوسط الصحي",
                        DescriptionEn = "Adopt the healthy Mediterranean lifestyle",
                        Difficulty = "easy",
                        DietType = "mediterranean",
                        Emoji = "🫒",
                        TasksJson = """[{"day":1,"taskAr":"استبدل الزبدة بزيت الزيتون","taskEn":"Replace butter with olive oil"},{"day":2,"taskAr":"تناول سمكاً للغداء أو العشاء","taskEn":"Eat fish for lunch or dinner"},{"day":3,"taskAr":"أضف طبقاً من الحمص إلى وجبتك","taskEn":"Add hummus to your meal"},{"day":4,"taskAr":"تناول سلطة كبيرة مع كل وجبة","taskEn":"Eat a large salad with every meal"},{"day":5,"taskAr":"جرب وصفة نباتية جديدة","taskEn":"Try a new vegetarian recipe"},{"day":6,"taskAr":"تناول العشاء مع عائلتك","taskEn":"Have dinner with your family"},{"day":7,"taskAr":"تمشّى 45 دقيقة في الهواء الطلق","taskEn":"Walk 45 minutes outdoors"}]"""
                    },
                    new()
                    {
                        Slug = "sugar-free-challenge",
                        NameAr = "تحدي بلا سكر",
                        NameEn = "Sugar-Free Challenge",
                        DescriptionAr = "تخلص من السكر المضاف لمدة 30 يوماً",
                        DescriptionEn = "Eliminate added sugar for 30 days",
                        Difficulty = "medium",
                        DietType = "default",
                        Emoji = "🚫🍬",
                        TasksJson = """[{"day":1,"taskAr":"تحقق من ملصقات الطعام واستبعد أي منتج يحتوي سكراً مضافاً","taskEn":"Check food labels and avoid any product with added sugar"},{"day":2,"taskAr":"استبدل المشروبات السكرية بالماء والأعشاب","taskEn":"Replace sugary drinks with water and herbs"},{"day":3,"taskAr":"جرب حلوى بالفواكه الطازجة فقط","taskEn":"Try a dessert with only fresh fruits"},{"day":4,"taskAr":"اطبخ وجبتك من الصفر بدون سكر","taskEn":"Cook your meal from scratch without sugar"},{"day":5,"taskAr":"ابحث عن بدائل صحية للحلويات","taskEn":"Find healthy alternatives to sweets"},{"day":6,"taskAr":"شارك إنجازك مع صديق","taskEn":"Share your achievement with a friend"},{"day":7,"taskAr":"احتفل بأسبوع بلا سكر","taskEn":"Celebrate one week sugar-free"}]"""
                    },
                    new()
                    {
                        Slug = "vegan-challenge",
                        NameAr = "تحدي النظام النباتي",
                        NameEn = "Vegan Challenge",
                        DescriptionAr = "جرّب النظام النباتي الكامل لمدة 30 يوماً",
                        DescriptionEn = "Try a fully plant-based diet for 30 days",
                        Difficulty = "medium",
                        DietType = "vegan",
                        Emoji = "🌱",
                        TasksJson = """[{"day":1,"taskAr":"استبدل الحليب بحليب اللوز أو الصويا","taskEn":"Replace dairy milk with almond or soy milk"},{"day":2,"taskAr":"جرب وصفة نباتية جديدة","taskEn":"Try a new vegan recipe"},{"day":3,"taskAr":"زر متجراً للأطعمة النباتية","taskEn":"Visit a health food store"},{"day":4,"taskAr":"تناول 5 حصص من الخضار والفواكه","taskEn":"Eat 5 servings of vegetables and fruits"},{"day":5,"taskAr":"جرب البروتين النباتي (توفو، تيمبيه)","taskEn":"Try plant-based protein (tofu, tempeh)"},{"day":6,"taskAr":"اطبخ وجبة نباتية للعائلة","taskEn":"Cook a vegan meal for the family"},{"day":7,"taskAr":"ساعد صديقاً على تجربة وجبة نباتية","taskEn":"Help a friend try a vegan meal"}]"""
                    },
                    new()
                    {
                        Slug = "if-challenge",
                        NameAr = "تحدي الصيام المتقطع",
                        NameEn = "Intermittent Fasting Challenge",
                        DescriptionAr = "طبّق نظام 16:8 لمدة 30 يوماً",
                        DescriptionEn = "Apply the 16:8 method for 30 days",
                        Difficulty = "medium",
                        DietType = "intermittent-fasting",
                        Emoji = "⏰",
                        TasksJson = """[{"day":1,"taskAr":"صم 16 ساعة مع شرب الماء فقط","taskEn":"Fast for 16 hours with water only"},{"day":2,"taskAr":"كسر الصيام بوجبة متوازنة","taskEn":"Break your fast with a balanced meal"},{"day":3,"taskAr":"تمرن في نهاية فترة الصيام","taskEn":"Exercise at the end of fasting period"},{"day":4,"taskAr":"خطط لنافذة الأكل مسبقاً","taskEn":"Plan your eating window in advance"},{"day":5,"taskAr":"جرب قهوة سوداء خلال الصيام","taskEn":"Try black coffee during fasting"},{"day":6,"taskAr":"تحقق من كيفية شعورك بعد الصيام","taskEn":"Check how you feel after fasting"},{"day":7,"taskAr":"سجّل تجربتك وشاركها","taskEn":"Journal your experience and share it"}]"""
                    },
                    new()
                    {
                        Slug = "paleo-challenge",
                        NameAr = "تحدي الباليو",
                        NameEn = "Paleo Challenge",
                        DescriptionAr = "عُد إلى الأكل الطبيعي الأصيل مع الباليو",
                        DescriptionEn = "Return to natural, authentic eating with Paleo",
                        Difficulty = "hard",
                        DietType = "paleo",
                        Emoji = "🥩",
                        TasksJson = """[{"day":1,"taskAr":"تخلص من جميع الحبوب والبقوليات من خزانتك","taskEn":"Remove all grains and legumes from your pantry"},{"day":2,"taskAr":"اطبخ وجبة باليو من اللحم والخضار","taskEn":"Cook a paleo meal with meat and vegetables"},{"day":3,"taskAr":"استبدل وجبة خفيفة بمكسرات وفاكهة","taskEn":"Replace a snack with nuts and fruit"},{"day":4,"taskAr":"تمرن لمدة 45 دقيقة","taskEn":"Exercise for 45 minutes"},{"day":5,"taskAr":"جرب طريقة طهي جديدة للحوم","taskEn":"Try a new meat cooking method"},{"day":6,"taskAr":"زر سوق المزارعين","taskEn":"Visit a farmers market"},{"day":7,"taskAr":"جهّز وجبات الأسبوع القادم","taskEn":"Prep next week's meals"}]"""
                    }
                };

                await db.Challenges.AddRangeAsync(challenges);
                await db.SaveChangesAsync();
            }

            // Seed community recipes
            if (!db.CommunityRecipes.Any())
            {
                var adminUser = await userManager.FindByEmailAsync("admin@moodbite.com");
                if (adminUser != null)
                {
                    var recipes = new List<CommunityRecipe>
                    {
                        new()
                        {
                            UserId = adminUser.Id,
                            TitleAr = "سلطة الكيتو بالأفوكادو والدجاج",
                            TitleEn = "Keto Avocado Chicken Salad",
                            DietType = "keto",
                            Calories = 520,
                            Protein = 42,
                            Carbs = 8,
                            Fats = 35,
                            PrepTime = 15,
                            Likes = 48,
                            IsApproved = true,
                            IngredientsJson = """[{"nameAr":"صدر دجاج مشوي (200 غرام)","nameEn":"Grilled chicken breast (200g)"},{"nameAr":"أفوكادو (1 حبة)","nameEn":"Avocado (1 piece)"},{"nameAr":"خس روماني","nameEn":"Romaine lettuce"},{"nameAr":"جبن فيتا","nameEn":"Feta cheese"},{"nameAr":"طماطم كرزية","nameEn":"Cherry tomatoes"},{"nameAr":"زيت زيتون (2 ملعقة)","nameEn":"Olive oil (2 tbsp)"},{"nameAr":"عصير ليمون","nameEn":"Lemon juice"},{"nameAr":"ملح وفلفل","nameEn":"Salt and pepper"}]""",
                            StepsJson = """[{"stepAr":"اشوِ صدر الدجاج مع الملح والفلفل والتوابل","stepEn":"Grill chicken breast with salt, pepper and spices"},{"stepAr":"قطّع الأفوكادو والطماطم","stepEn":"Dice avocado and tomatoes"},{"stepAr":"افرم الخس وضعه في الطبق","stepEn":"Chop lettuce and place in bowl"},{"stepAr":"أضف الدجاج والأفوكادو والطماطم","stepEn":"Add chicken, avocado and tomatoes"},{"stepAr":"اسكب زيت الزيتون وعصير الليمون","stepEn":"Drizzle olive oil and lemon juice"},{"stepAr":"رشّ الجبن الفيتا وقدّم","stepEn":"Sprinkle feta cheese and serve"}]"""
                        },
                        new()
                        {
                            UserId = adminUser.Id,
                            TitleAr = "شكشوكة البيض على الطريقة المتوسطية",
                            TitleEn = "Mediterranean Shakshuka",
                            DietType = "mediterranean",
                            Calories = 340,
                            Protein = 20,
                            Carbs = 25,
                            Fats = 18,
                            PrepTime = 25,
                            Likes = 62,
                            IsApproved = true,
                            IngredientsJson = """[{"nameAr":"بيض (4 حبات)","nameEn":"Eggs (4 pieces)"},{"nameAr":"طماطم محروقة (400 غرام)","nameEn":"Crushed tomatoes (400g)"},{"nameAr":"فلفل أحمر (1 حبة)","nameEn":"Red bell pepper (1)"},{"nameAr":"بصل (1 حبة)","nameEn":"Onion (1)"},{"nameAr":"ثوم (3 فصوص)","nameEn":"Garlic (3 cloves)"},{"nameAr":"كمون وكزبرة وبابريكا","nameEn":"Cumin, coriander, paprika"},{"nameAr":"زيت زيتون","nameEn":"Olive oil"},{"nameAr":"بقدونس طازج","nameEn":"Fresh parsley"}]""",
                            StepsJson = """[{"stepAr":"سخّن زيت الزيتون في المقلاة","stepEn":"Heat olive oil in pan"},{"stepAr":"أضف البصل والفلفل وقلّب حتى يلين","stepEn":"Add onion and pepper, cook until soft"},{"stepAr":"أضف الثوم والتوابل وقلّب دقيقة","stepEn":"Add garlic and spices, stir for 1 minute"},{"stepAr":"أضف الطماطم واطبخ 10 دقائق","stepEn":"Add tomatoes and cook 10 minutes"},{"stepAr":"اكسر البيض فوق الصلصة","stepEn":"Crack eggs over the sauce"},{"stepAr":"غطّ وانتظر حتى ينضج البيض","stepEn":"Cover and wait until eggs are cooked"},{"stepAr":"زيّن بالبقدونس وقدّم","stepEn":"Garnish with parsley and serve"}]"""
                        },
                        new()
                        {
                            UserId = adminUser.Id,
                            TitleAr = "شوفان التوت الصحي",
                            TitleEn = "Healthy Vegan Berry Oatmeal",
                            DietType = "vegan",
                            Calories = 380,
                            Protein = 12,
                            Carbs = 65,
                            Fats = 8,
                            PrepTime = 10,
                            Likes = 35,
                            IsApproved = true,
                            IngredientsJson = """[{"nameAr":"شوفان ملفوف (80 غرام)","nameEn":"Rolled oats (80g)"},{"nameAr":"حليب اللوز (250 مل)","nameEn":"Almond milk (250ml)"},{"nameAr":"توت طازج أو مجمد (100 غرام)","nameEn":"Fresh or frozen berries (100g)"},{"nameAr":"موز (1 حبة)","nameEn":"Banana (1)"},{"nameAr":"بذور الشيا (1 ملعقة)","nameEn":"Chia seeds (1 tbsp)"},{"nameAr":"عسل النحل (1 ملعقة)","nameEn":"Honey (1 tbsp)"},{"nameAr":"قرفة","nameEn":"Cinnamon"}]""",
                            StepsJson = """[{"stepAr":"اسلق الشوفان في حليب اللوز","stepEn":"Cook oats in almond milk"},{"stepAr":"أضف قطع الموز وبذور الشيا","stepEn":"Add banana slices and chia seeds"},{"stepAr":"قلّب حتى يصبح القوام كريمياً","stepEn":"Stir until creamy consistency"},{"stepAr":"انقله إلى طبق وضع التوت فوقه","stepEn":"Transfer to bowl and top with berries"},{"stepAr":"أضف العسل والقرفة","stepEn":"Add honey and cinnamon"},{"stepAr":"قدّم فوراً","stepEn":"Serve immediately"}]"""
                        },
                        new()
                        {
                            UserId = adminUser.Id,
                            TitleAr = "سلمون مشوي مع الليمون والأعشاب",
                            TitleEn = "Lemon Herb Grilled Salmon",
                            DietType = "paleo",
                            Calories = 480,
                            Protein = 45,
                            Carbs = 5,
                            Fats = 30,
                            PrepTime = 20,
                            Likes = 71,
                            IsApproved = true,
                            IngredientsJson = """[{"nameAr":"فيليه سلمون (200 غرام)","nameEn":"Salmon fillet (200g)"},{"nameAr":"ليمون (1 حبة)","nameEn":"Lemon (1)"},{"nameAr":"ثوم (2 فص)","nameEn":"Garlic (2 cloves)"},{"nameAr":"زعتر طازج","nameEn":"Fresh thyme"},{"nameAr":"إكليل الجبل","nameEn":"Rosemary"},{"nameAr":"زيت زيتون (2 ملعقة)","nameEn":"Olive oil (2 tbsp)"},{"nameAr":"ملح البحر والفلفل","nameEn":"Sea salt and pepper"},{"nameAr":"أسباراغوس للتقديم","nameEn":"Asparagus for serving"}]""",
                            StepsJson = """[{"stepAr":"اخلط زيت الزيتون مع الثوم والأعشاب وعصير الليمون","stepEn":"Mix olive oil with garlic, herbs and lemon juice"},{"stepAr":"تبّل السلمون بالخليط واتركه 15 دقيقة","stepEn":"Marinate salmon for 15 minutes"},{"stepAr":"سخّن الشواية على حرارة عالية","stepEn":"Heat grill to high temperature"},{"stepAr":"اشوِ السلمون 4 دقائق من كل جانب","stepEn":"Grill salmon 4 minutes each side"},{"stepAr":"اشوِ الأسباراغوس بجانبه","stepEn":"Grill asparagus alongside"},{"stepAr":"قدّم مع شرائح الليمون والأعشاب","stepEn":"Serve with lemon slices and herbs"}]"""
                        },
                        new()
                        {
                            UserId = adminUser.Id,
                            TitleAr = "شوربة العدس الأحمر",
                            TitleEn = "Red Lentil Soup",
                            DietType = "mediterranean",
                            Calories = 380,
                            Protein = 20,
                            Carbs = 58,
                            Fats = 8,
                            PrepTime = 30,
                            Likes = 55,
                            IsApproved = true,
                            IngredientsJson = """[{"nameAr":"عدس أحمر (200 غرام)","nameEn":"Red lentils (200g)"},{"nameAr":"بصل (2 حبة)","nameEn":"Onion (2)"},{"nameAr":"ثوم (4 فصوص)","nameEn":"Garlic (4 cloves)"},{"nameAr":"طماطم (2 حبة)","nameEn":"Tomatoes (2)"},{"nameAr":"كمون وكركم وكزبرة","nameEn":"Cumin, turmeric, coriander"},{"nameAr":"مرق دجاج أو خضار","nameEn":"Chicken or vegetable broth"},{"nameAr":"زيت زيتون","nameEn":"Olive oil"},{"nameAr":"عصير ليمون","nameEn":"Lemon juice"}]""",
                            StepsJson = """[{"stepAr":"قلّي البصل بزيت الزيتون حتى يتحمر","stepEn":"Fry onion in olive oil until golden"},{"stepAr":"أضف الثوم والتوابل وقلّب","stepEn":"Add garlic and spices, stir"},{"stepAr":"أضف الطماطم واطبخ 5 دقائق","stepEn":"Add tomatoes and cook 5 minutes"},{"stepAr":"أضف العدس المغسول والمرق","stepEn":"Add washed lentils and broth"},{"stepAr":"اطبخ 20 دقيقة على نار هادئة","stepEn":"Cook 20 minutes on low heat"},{"stepAr":"خمّر الشوربة حتى تصبح ناعمة","stepEn":"Blend soup until smooth"},{"stepAr":"أضف عصير الليمون وقدّم","stepEn":"Add lemon juice and serve"}]"""
                        },
                        new()
                        {
                            UserId = adminUser.Id,
                            TitleAr = "لحم بقري بالخضار على الطريقة الباليو",
                            TitleEn = "Paleo Beef and Vegetables",
                            DietType = "paleo",
                            Calories = 560,
                            Protein = 48,
                            Carbs = 18,
                            Fats = 32,
                            PrepTime = 35,
                            Likes = 43,
                            IsApproved = true,
                            IngredientsJson = """[{"nameAr":"لحم بقري (250 غرام)","nameEn":"Beef (250g)"},{"nameAr":"فلفل ملون (3 حبات)","nameEn":"Colored peppers (3)"},{"nameAr":"كوسة (2 حبة)","nameEn":"Zucchini (2)"},{"nameAr":"بصل (1 حبة)","nameEn":"Onion (1)"},{"nameAr":"ثوم (3 فصوص)","nameEn":"Garlic (3 cloves)"},{"nameAr":"زيت جوز الهند","nameEn":"Coconut oil"},{"nameAr":"صلصة تاماري","nameEn":"Tamari sauce"},{"nameAr":"زنجبيل طازج","nameEn":"Fresh ginger"}]""",
                            StepsJson = """[{"stepAr":"قطّع اللحم إلى شرائح رفيعة","stepEn":"Slice beef into thin strips"},{"stepAr":"تبّله بالتاماري والزنجبيل والثوم","stepEn":"Marinate with tamari, ginger and garlic"},{"stepAr":"سخّن زيت جوز الهند في مقلاة عميقة","stepEn":"Heat coconut oil in deep pan"},{"stepAr":"اشوِ اللحم على نار عالية","stepEn":"Sear beef on high heat"},{"stepAr":"أضف الخضروات وقلّب بسرعة","stepEn":"Add vegetables and stir quickly"},{"stepAr":"اطبخ 5-7 دقائق مع التقليب المستمر","stepEn":"Cook 5-7 minutes while stirring"},{"stepAr":"قدّم فوراً","stepEn":"Serve immediately"}]"""
                        }
                    };

                    await db.CommunityRecipes.AddRangeAsync(recipes);
                    await db.SaveChangesAsync();
                }
            }

            // Seed restaurants — re-seed if fewer than 40 records (replaces old placeholder data)
            if (db.Restaurants.Count() < 40)
            {
                db.Restaurants.RemoveRange(db.Restaurants);
                await db.SaveChangesAsync();

                var restaurants = new List<Restaurant>
                {
                    // ── Jordan — Amman ──
                    new() { NameAr = "فخر الدين", NameEn = "Fakhr El-Din", Type = "mediterranean", City = "Amman", Country = "Jordan", Latitude = 31.9653, Longitude = 35.9091, Rating = 4.8, Phone = "+962-6-4652399", Address = "شارع المتنبي، الدوار الثالث، عمّان" },
                    new() { NameAr = "سفرة", NameEn = "Sufra Restaurant", Type = "healthy", City = "Amman", Country = "Jordan", Latitude = 31.9542, Longitude = 35.9009, Rating = 4.7, Phone = "+962-6-4612650", Address = "شارع الرينبو، جبل عمّان" },
                    new() { NameAr = "وايلد جوردان كافيه", NameEn = "Wild Jordan Center Café", Type = "vegan", City = "Amman", Country = "Jordan", Latitude = 31.9550, Longitude = 35.9038, Rating = 4.5, Phone = "+962-6-4633542", Address = "حديقة الملك الحسين، جبل عمّان" },
                    new() { NameAr = "جوز هند", NameEn = "Joz Hind", Type = "healthy", City = "Amman", Country = "Jordan", Latitude = 31.9543, Longitude = 35.8987, Rating = 4.4, Phone = "+962-6-4616060", Address = "اللويبدة، عمّان" },
                    new() { NameAr = "بون كافيه", NameEn = "Boon Café", Type = "healthy", City = "Amman", Country = "Jordan", Latitude = 31.9761, Longitude = 35.8648, Rating = 4.6, Phone = "+962-6-5926666", Address = "عبدون، عمّان" },
                    new() { NameAr = "سيكراب كافيه", NameEn = "Sekrab Café", Type = "vegan", City = "Amman", Country = "Jordan", Latitude = 31.9642, Longitude = 35.8706, Rating = 4.3, Phone = "+962-6-5820006", Address = "الصويفية، عمّان" },
                    new() { NameAr = "مطعم الكيتو", NameEn = "Keto Kitchen JO", Type = "keto", City = "Amman", Country = "Jordan", Latitude = 31.9839, Longitude = 35.8696, Rating = 4.4, Phone = "+962-79-9999001", Address = "دوار عبدون، عمّان" },
                    new() { NameAr = "مشاوير الصحة", NameEn = "Mashaweer Health Grill", Type = "healthy", City = "Amman", Country = "Jordan", Latitude = 31.9499, Longitude = 35.9342, Rating = 4.2, Phone = "+962-6-4639000", Address = "جبل اللويبدة، عمّان" },
                    new() { NameAr = "كافيه ريم", NameEn = "Reem Café & Kitchen", Type = "mediterranean", City = "Amman", Country = "Jordan", Latitude = 31.9604, Longitude = 35.8644, Rating = 4.5, Phone = "+962-6-5813030", Address = "الصويفية، عمّان" },
                    new() { NameAr = "ذا فولت عمّان", NameEn = "The Vault Amman", Type = "healthy", City = "Amman", Country = "Jordan", Latitude = 31.9538, Longitude = 35.9100, Rating = 4.6, Phone = "+962-6-4641411", Address = "شارع الرينبو، عمّان" },

                    // ── Saudi Arabia — Riyadh ──
                    new() { NameAr = "سالاد ستوب الرياض", NameEn = "SaladStop! Riyadh", Type = "healthy", City = "Riyadh", Country = "Saudi Arabia", Latitude = 24.7455, Longitude = 46.6680, Rating = 4.4, Phone = "+966-11-2931000", Address = "بارك أفينيو، طريق الملك فهد، الرياض" },
                    new() { NameAr = "كيكال الرياض", NameEn = "Kcal Riyadh", Type = "healthy", City = "Riyadh", Country = "Saudi Arabia", Latitude = 24.7100, Longitude = 46.6700, Rating = 4.5, Phone = "+966-11-2131234", Address = "شارع التحلية، الرياض" },
                    new() { NameAr = "المطبخ الأخضر", NameEn = "The Green Kitchen Riyadh", Type = "vegan", City = "Riyadh", Country = "Saudi Arabia", Latitude = 24.6936, Longitude = 46.6862, Rating = 4.3, Phone = "+966-11-4656565", Address = "العليا، الرياض" },
                    new() { NameAr = "بيكر آند سبايس الرياض", NameEn = "Baker & Spice Riyadh", Type = "healthy", City = "Riyadh", Country = "Saudi Arabia", Latitude = 24.6877, Longitude = 46.7219, Rating = 4.6, Phone = "+966-11-2199999", Address = "المربع الرياض" },
                    new() { NameAr = "إليفيشن برجر", NameEn = "Elevation Burger Riyadh", Type = "healthy", City = "Riyadh", Country = "Saudi Arabia", Latitude = 24.7274, Longitude = 46.6432, Rating = 4.2, Phone = "+966-11-2648787", Address = "لومار مول، الرياض" },
                    new() { NameAr = "سيزونز ريستوراند", NameEn = "Seasons Restaurant Riyadh", Type = "mediterranean", City = "Riyadh", Country = "Saudi Arabia", Latitude = 24.6765, Longitude = 46.6986, Rating = 4.7, Phone = "+966-11-4882200", Address = "الحي الدبلوماسي، الرياض" },
                    new() { NameAr = "هاشي", NameEn = "Hashi Restaurant Riyadh", Type = "healthy", City = "Riyadh", Country = "Saudi Arabia", Latitude = 24.7143, Longitude = 46.6753, Rating = 4.5, Phone = "+966-11-2356767", Address = "حي النخيل، الرياض" },
                    new() { NameAr = "زوي ريستورانت", NameEn = "Zoë Restaurant Riyadh", Type = "mediterranean", City = "Riyadh", Country = "Saudi Arabia", Latitude = 24.7366, Longitude = 46.6601, Rating = 4.4, Phone = "+966-11-4778899", Address = "غرناطة مول، الرياض" },
                    new() { NameAr = "غرين هاوس", NameEn = "Greenhouse Salad Bar", Type = "vegan", City = "Riyadh", Country = "Saudi Arabia", Latitude = 24.6993, Longitude = 46.7310, Rating = 4.3, Phone = "+966-11-4012020", Address = "حي الملز، الرياض" },
                    new() { NameAr = "كيتو دايت هاوس", NameEn = "Keto Diet House Riyadh", Type = "keto", City = "Riyadh", Country = "Saudi Arabia", Latitude = 24.7500, Longitude = 46.6900, Rating = 4.2, Phone = "+966-55-1234567", Address = "حي السليمانية، الرياض" },

                    // ── UAE — Dubai ──
                    new() { NameAr = "وايلد آند ذا مون", NameEn = "Wild & The Moon Dubai", Type = "vegan", City = "Dubai", Country = "UAE", Latitude = 25.2014, Longitude = 55.2750, Rating = 4.7, Phone = "+971-4-3882522", Address = "مرسى دبي" },
                    new() { NameAr = "كومبتوار ١٠٢", NameEn = "Comptoir 102", Type = "vegan", City = "Dubai", Country = "UAE", Latitude = 25.2040, Longitude = 55.2560, Rating = 4.6, Phone = "+971-4-3852102", Address = "شارع جميرا، دبي" },
                    new() { NameAr = "كيكال دبي مارينا", NameEn = "Kcal Dubai Marina", Type = "healthy", City = "Dubai", Country = "UAE", Latitude = 25.2048, Longitude = 55.2708, Rating = 4.5, Phone = "+971-4-4536000", Address = "دبي مارينا مول" },
                    new() { NameAr = "باونتي بيتس", NameEn = "Bounty Beets", Type = "vegan", City = "Dubai", Country = "UAE", Latitude = 25.2113, Longitude = 55.2844, Rating = 4.4, Phone = "+971-4-3990055", Address = "الرصيف، جي بي آر، دبي" },
                    new() { NameAr = "ذا فارم دبي", NameEn = "The Farm Dubai", Type = "healthy", City = "Dubai", Country = "UAE", Latitude = 25.0670, Longitude = 55.1740, Rating = 4.8, Phone = "+971-4-3618000", Address = "البراري، دبي" },
                    new() { NameAr = "بيكر آند سبايس دبي", NameEn = "Baker & Spice Dubai", Type = "healthy", City = "Dubai", Country = "UAE", Latitude = 25.2013, Longitude = 55.2734, Rating = 4.6, Phone = "+971-4-4251900", Address = "واجهة دبي مارينا" },
                    new() { NameAr = "آروز آند سبارووز", NameEn = "Arrows & Sparrows", Type = "healthy", City = "Dubai", Country = "UAE", Latitude = 25.2062, Longitude = 55.2725, Rating = 4.5, Phone = "+971-4-4532688", Address = "مرسى دبي" },
                    new() { NameAr = "هيلذي ووك", NameEn = "Healthy Wok JLT", Type = "healthy", City = "Dubai", Country = "UAE", Latitude = 25.1924, Longitude = 55.2685, Rating = 4.3, Phone = "+971-4-4530303", Address = "جبل علي ليك تاورز، دبي" },
                    new() { NameAr = "كيتو دبي كيتشن", NameEn = "Keto Dubai Kitchen", Type = "keto", City = "Dubai", Country = "UAE", Latitude = 25.1890, Longitude = 55.2770, Rating = 4.4, Phone = "+971-50-1234567", Address = "الخيل هايتس، دبي" },
                    new() { NameAr = "سوشيال كيتشن دبي", NameEn = "Social Kitchen Dubai", Type = "mediterranean", City = "Dubai", Country = "UAE", Latitude = 25.1990, Longitude = 55.2740, Rating = 4.6, Phone = "+971-4-3990077", Address = "واجهة دبي مارينا ووك" },

                    // ── Egypt — Cairo ──
                    new() { NameAr = "زوبا", NameEn = "Zooba", Type = "healthy", City = "Cairo", Country = "Egypt", Latitude = 30.0557, Longitude = 31.2396, Rating = 4.7, Phone = "+20-2-27360024", Address = "شارع 26 يوليو، الزمالك، القاهرة" },
                    new() { NameAr = "سيكويا القاهرة", NameEn = "Sequoia Cairo", Type = "healthy", City = "Cairo", Country = "Egypt", Latitude = 30.0564, Longitude = 31.2181, Rating = 4.8, Phone = "+20-2-27352834", Address = "كورنيش النيل، أجوزة، القاهرة" },
                    new() { NameAr = "سوشي سان", NameEn = "Sushi San Cairo", Type = "healthy", City = "Cairo", Country = "Egypt", Latitude = 30.0544, Longitude = 31.2236, Rating = 4.5, Phone = "+20-2-27350070", Address = "شارع الجزيرة، الزمالك، القاهرة" },
                    new() { NameAr = "سوق السمك", NameEn = "The Fish Market Cairo", Type = "healthy", City = "Cairo", Country = "Egypt", Latitude = 30.0578, Longitude = 31.2183, Rating = 4.6, Phone = "+20-2-33380088", Address = "كورنيش النيل، أجوزة، القاهرة" },
                    new() { NameAr = "كافيه كيتو مصر", NameEn = "Keto Egypt Café", Type = "keto", City = "Cairo", Country = "Egypt", Latitude = 30.0602, Longitude = 31.2338, Rating = 4.3, Phone = "+20-10-09876543", Address = "شارع محمد مظهر، الزمالك، القاهرة" },
                    new() { NameAr = "كايرو كيتشن", NameEn = "Cairo Kitchen", Type = "healthy", City = "Cairo", Country = "Egypt", Latitude = 30.0565, Longitude = 31.2395, Rating = 4.4, Phone = "+20-2-27358200", Address = "شارع أبو الفدا، الزمالك، القاهرة" },
                    new() { NameAr = "أنتركوت القاهرة", NameEn = "Entrecote Cairo", Type = "healthy", City = "Cairo", Country = "Egypt", Latitude = 30.0510, Longitude = 31.2340, Rating = 4.5, Phone = "+20-2-27362601", Address = "ميدان أسوان، الزمالك، القاهرة" },
                    new() { NameAr = "بول القاهرة", NameEn = "Paul's Cairo", Type = "healthy", City = "Cairo", Country = "Egypt", Latitude = 30.0623, Longitude = 31.2156, Rating = 4.3, Phone = "+20-2-33034070", Address = "شارع جامعة الدول العربية، المهندسين، القاهرة" },
                    new() { NameAr = "المزرعة العضوية", NameEn = "Organic Farm Cairo", Type = "vegan", City = "Cairo", Country = "Egypt", Latitude = 30.0434, Longitude = 31.2235, Rating = 4.4, Phone = "+20-2-37606065", Address = "شارع التحرير، الدقي، القاهرة" },
                    new() { NameAr = "نيل سيتي رستوران", NameEn = "Nile City Restaurant", Type = "mediterranean", City = "Cairo", Country = "Egypt", Latitude = 30.0480, Longitude = 31.2260, Rating = 4.6, Phone = "+20-2-24614800", Address = "نايل سيتي تاورز، القاهرة" },

                    // ── Kuwait — Kuwait City ──
                    new() { NameAr = "مطعم ميس الغانم", NameEn = "Mais Alghanim", Type = "healthy", City = "Kuwait City", Country = "Kuwait", Latitude = 29.3375, Longitude = 47.9897, Rating = 4.7, Phone = "+965-2245-5555", Address = "منطقة الشرق، الكويت" },
                    new() { NameAr = "سوق السمك الكويت", NameEn = "Fish Market Kuwait", Type = "healthy", City = "Kuwait City", Country = "Kuwait", Latitude = 29.3679, Longitude = 47.9680, Rating = 4.5, Phone = "+965-2245-6666", Address = "منطقة الميناء، الكويت" },
                    new() { NameAr = "فلامنكي الكويت", NameEn = "Falamanki Kuwait", Type = "mediterranean", City = "Kuwait City", Country = "Kuwait", Latitude = 29.3493, Longitude = 47.9828, Rating = 4.4, Phone = "+965-2223-0009", Address = "مارينا كريسنت، السالمية" },
                    new() { NameAr = "أورغانيك كويت", NameEn = "Organic Kuwait", Type = "vegan", City = "Kuwait City", Country = "Kuwait", Latitude = 29.3330, Longitude = 47.9910, Rating = 4.3, Phone = "+965-2230-2222", Address = "منطقة الشرق، الكويت" },
                    new() { NameAr = "سلايدر ستيشن", NameEn = "Slider Station Kuwait", Type = "healthy", City = "Kuwait City", Country = "Kuwait", Latitude = 29.3401, Longitude = 47.9823, Rating = 4.5, Phone = "+965-2224-1111", Address = "مارينا كريسنت، الكويت" },
                    new() { NameAr = "فريش كيتشن", NameEn = "Fresh Kitchen Kuwait", Type = "healthy", City = "Kuwait City", Country = "Kuwait", Latitude = 29.3420, Longitude = 47.9869, Rating = 4.2, Phone = "+965-2241-3333", Address = "منطقة الصالحية، الكويت" },
                    new() { NameAr = "المشاوي الكويتية", NameEn = "Kuwait Mixed Grill", Type = "healthy", City = "Kuwait City", Country = "Kuwait", Latitude = 29.3600, Longitude = 47.9750, Rating = 4.4, Phone = "+965-2573-5555", Address = "السالمية، الكويت" },
                    new() { NameAr = "بيبلوس الكويت", NameEn = "Byblos Lebanese Grill", Type = "mediterranean", City = "Kuwait City", Country = "Kuwait", Latitude = 29.3493, Longitude = 47.9817, Rating = 4.6, Phone = "+965-2564-7777", Address = "شارع عبدالله المبارك، السالمية" },
                    new() { NameAr = "كيتو هاوس الكويت", NameEn = "Keto House Kuwait", Type = "keto", City = "Kuwait City", Country = "Kuwait", Latitude = 29.3452, Longitude = 47.9840, Rating = 4.3, Phone = "+965-9911-2233", Address = "حولي، الكويت" },
                    new() { NameAr = "زعفران الكويت", NameEn = "Saffron Restaurant Kuwait", Type = "healthy", City = "Kuwait City", Country = "Kuwait", Latitude = 29.3375, Longitude = 48.0045, Rating = 4.5, Phone = "+965-2532-9900", Address = "الجابرية، الكويت" }
                };

                await db.Restaurants.AddRangeAsync(restaurants);
                await db.SaveChangesAsync();
            }

            if (seedDemoData)
            {
                await SeedMarketDemoDataAsync(services);
                await SeedHamzaDataAsync(services);
            }
        }

        private static async Task SeedMarketDemoDataAsync(IServiceProvider services)
        {
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var db = services.GetRequiredService<ApplicationDbContext>();
            var mealPlanService = services.GetRequiredService<MealPlanService>();

            const string demoPassword = "Demo@123456";
            var admin = await EnsureDemoUserAsync(
                userManager,
                "admin@moodbite.com",
                "MoodBite Admin",
                ApplicationRoles.Admin,
                demoPassword,
                preferredLanguage: "en");
            var owner = await EnsureDemoUserAsync(
                userManager,
                "clinic.owner@moodbite.demo",
                "Dr. Sara Clinic Owner",
                ApplicationRoles.ClinicOwner,
                demoPassword,
                preferredLanguage: "en");
            var dietitian = await EnsureDemoUserAsync(
                userManager,
                "dietitian@moodbite.demo",
                "Lina Dietitian",
                ApplicationRoles.Dietitian,
                demoPassword,
                preferredLanguage: "en");
            var staff = await EnsureDemoUserAsync(
                userManager,
                "staff@moodbite.demo",
                "Omar Clinic Staff",
                ApplicationRoles.ClinicStaff,
                demoPassword,
                preferredLanguage: "en");
            var patientOne = await EnsureDemoUserAsync(
                userManager,
                "patient.one@moodbite.demo",
                "Maya Patient",
                ApplicationRoles.User,
                demoPassword,
                preferredLanguage: "en");
            var patientTwo = await EnsureDemoUserAsync(
                userManager,
                "patient.two@moodbite.demo",
                "Yousef Patient",
                ApplicationRoles.User,
                demoPassword,
                preferredLanguage: "ar");

            var clinic = db.Clinics.FirstOrDefault(c => c.Slug == "demo-wellness-clinic");
            if (clinic == null)
            {
                clinic = new Clinic
                {
                    Name = "MoodBite Wellness Clinic",
                    Slug = "demo-wellness-clinic",
                    LegalName = "MoodBite Wellness Clinic LLC",
                    Email = "hello@moodbite.demo",
                    Phone = "+962-6-555-0101",
                    Country = "Jordan",
                    City = "Amman",
                    Address = "Demo Street, Abdoun",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-45)
                };
                db.Clinics.Add(clinic);
                await db.SaveChangesAsync();
            }

            await EnsureClinicMemberAsync(db, clinic.Id, owner.Id, ApplicationRoles.ClinicOwner, admin.Id);
            await EnsureClinicMemberAsync(db, clinic.Id, dietitian.Id, ApplicationRoles.Dietitian, owner.Id);
            await EnsureClinicMemberAsync(db, clinic.Id, staff.Id, ApplicationRoles.ClinicStaff, owner.Id);

            await EnsurePatientDemoDataAsync(
                db,
                mealPlanService,
                clinic,
                patientOne,
                dietitian,
                "mediterranean",
                "loseWeight",
                1650,
                72,
                168,
                34,
                seedOffset: 0);
            await EnsurePatientDemoDataAsync(
                db,
                mealPlanService,
                clinic,
                patientTwo,
                dietitian,
                "keto",
                "maintain",
                2100,
                86,
                178,
                42,
                seedOffset: 11);
        }

        private static async Task<ApplicationUser> EnsureDemoUserAsync(
            UserManager<ApplicationUser> userManager,
            string email,
            string fullName,
            string role,
            string password,
            string preferredLanguage)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    IsActive = true,
                    EmailConfirmed = true,
                    PreferredLanguage = preferredLanguage
                };

                var result = await userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Unable to create demo user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                user.FullName = string.IsNullOrWhiteSpace(user.FullName) ? fullName : user.FullName;
                user.IsActive = true;
                user.EmailConfirmed = true;
                user.PreferredLanguage ??= preferredLanguage;
                await userManager.UpdateAsync(user);
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }

            return user;
        }

        private static async Task EnsureClinicMemberAsync(
            ApplicationDbContext db,
            int clinicId,
            string userId,
            string role,
            string invitedByUserId)
        {
            var member = db.ClinicMembers.FirstOrDefault(m => m.ClinicId == clinicId && m.UserId == userId);
            if (member == null)
            {
                db.ClinicMembers.Add(new ClinicMember
                {
                    ClinicId = clinicId,
                    UserId = userId,
                    Role = role,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow.AddDays(-30),
                    InvitedByUserId = invitedByUserId
                });
            }
            else
            {
                member.Role = role;
                member.IsActive = true;
            }

            await db.SaveChangesAsync();
        }

        private static async Task EnsurePatientDemoDataAsync(
            ApplicationDbContext db,
            MealPlanService mealPlanService,
            Clinic clinic,
            ApplicationUser patient,
            ApplicationUser dietitian,
            string dietSlug,
            string goal,
            double calorieTarget,
            double startingWeight,
            double height,
            int age,
            int seedOffset)
        {
            var link = db.ClinicPatients.FirstOrDefault(p => p.ClinicId == clinic.Id && p.PatientId == patient.Id);
            if (link == null)
            {
                db.ClinicPatients.Add(new ClinicPatient
                {
                    ClinicId = clinic.Id,
                    PatientId = patient.Id,
                    PrimaryDietitianId = dietitian.Id,
                    Status = "active",
                    ConsentGranted = true,
                    ConsentGrantedAt = DateTime.UtcNow.AddDays(-20),
                    LinkedAt = DateTime.UtcNow.AddDays(-20),
                    InternalNotes = "Demo patient for clinic workflow."
                });
            }
            else
            {
                link.PrimaryDietitianId = dietitian.Id;
                link.Status = "active";
                link.ConsentGranted = true;
                link.ConsentGrantedAt ??= DateTime.UtcNow.AddDays(-20);
                link.ArchivedAt = null;
            }

            if (!db.HealthProfiles.Any(h => h.UserId == patient.Id))
            {
                db.HealthProfiles.Add(new HealthProfile
                {
                    UserId = patient.Id,
                    Age = age,
                    Gender = seedOffset == 0 ? "female" : "male",
                    Height = height,
                    Weight = startingWeight - 2.4,
                    Goal = goal,
                    ActivityLevel = "moderate",
                    HealthConditions = "[]",
                    Allergens = "[]",
                    FoodPreferences = "[\"home cooking\",\"quick lunches\"]",
                    CookingStyle = "moderate",
                    Budget = "medium",
                    DietSlug = dietSlug,
                    CalorieTarget = calorieTarget,
                    WaterGoal = seedOffset == 0 ? 8 : 9,
                    UpdatedAt = DateTime.UtcNow.AddDays(-2)
                });
            }

            if (db.WeightLogs.Count(w => w.UserId == patient.Id) < 4)
            {
                for (var i = 0; i < 6; i++)
                {
                    db.WeightLogs.Add(new WeightLog
                    {
                        UserId = patient.Id,
                        Date = DateTime.Today.AddDays(-35 + i * 7),
                        Weight = Math.Round(startingWeight - i * 0.45 - seedOffset * 0.03, 1),
                        Note = i == 0 ? "Demo baseline" : null
                    });
                }
            }

            if (db.DayLogs.Count(d => d.UserId == patient.Id) < 7)
            {
                for (var i = 0; i < 14; i++)
                {
                    var calories = calorieTarget + ((i % 5) - 2) * 85;
                    db.DayLogs.Add(new DayLog
                    {
                        UserId = patient.Id,
                        Date = DateTime.Today.AddDays(-13 + i),
                        CaloriesConsumed = calories,
                        CaloriesBurned = 220 + (i % 4) * 35,
                        Protein = 95 + (i % 5) * 6,
                        Carbs = dietSlug == "keto" ? 45 + (i % 3) * 5 : 165 + (i % 4) * 10,
                        Fats = dietSlug == "keto" ? 115 + (i % 4) * 7 : 58 + (i % 4) * 4,
                        Mood = i % 3 == 0 ? "energetic" : i % 3 == 1 ? "happy" : "focused",
                        Adherent = Math.Abs(calories - calorieTarget) / calorieTarget <= 0.15
                    });
                }
            }

            if (db.WaterLogs.Count(w => w.UserId == patient.Id) < 7)
            {
                for (var i = 0; i < 14; i++)
                {
                    db.WaterLogs.Add(new WaterLog
                    {
                        UserId = patient.Id,
                        Date = DateTime.Today.AddDays(-13 + i),
                        GlassesCount = 6 + ((i + seedOffset) % 4)
                    });
                }
            }

            if (db.FoodScans.Count(f => f.UserId == patient.Id) < 2)
            {
                db.FoodScans.AddRange(
                    new FoodScan
                    {
                        UserId = patient.Id,
                        ImagePath = string.Empty,
                        FoodNameAr = "سلطة دجاج",
                        FoodNameEn = "Chicken salad",
                        Confidence = 92,
                        Calories = 420,
                        Protein = 36,
                        Carbs = 22,
                        Fats = 18,
                        ServingSize = "1 bowl",
                        ServingSizeAr = "وعاء واحد",
                        DescriptionEn = "Demo scan with balanced protein and vegetables.",
                        DescriptionAr = "فحص تجريبي يحتوي على بروتين وخضار.",
                        LoggedToDashboard = true,
                        ScannedAt = DateTime.UtcNow.AddDays(-3)
                    },
                    new FoodScan
                    {
                        UserId = patient.Id,
                        ImagePath = string.Empty,
                        FoodNameAr = "زبادي يوناني",
                        FoodNameEn = "Greek yogurt",
                        Confidence = 88,
                        Calories = 180,
                        Protein = 18,
                        Carbs = 14,
                        Fats = 5,
                        ServingSize = "1 cup",
                        ServingSizeAr = "كوب واحد",
                        LoggedToDashboard = false,
                        ScannedAt = DateTime.UtcNow.AddDays(-1)
                    });
            }

            if (db.MealPlans.Count(m => m.UserId == patient.Id) < 2)
            {
                db.MealPlans.AddRange(
                    new MealPlan
                    {
                        UserId = patient.Id,
                        PlanType = "standard",
                        PlanJson = mealPlanService.GenerateWeekPlan(dietSlug, 100 + seedOffset),
                        Title = "Clinic standard plan",
                        DietType = dietSlug,
                        CalorieTarget = calorieTarget,
                        CreatedAt = DateTime.UtcNow.AddDays(-10)
                    },
                    new MealPlan
                    {
                        UserId = patient.Id,
                        PlanType = "ai",
                        PlanJson = mealPlanService.GenerateWeekPlan(dietSlug, 200 + seedOffset),
                        Title = "Demo AI-style plan",
                        DietType = dietSlug,
                        CalorieTarget = calorieTarget,
                        CreatedAt = DateTime.UtcNow.AddDays(-4)
                    });
            }

            if (db.ClinicalNotes.Count(n => n.ClinicId == clinic.Id && n.PatientId == patient.Id) < 2)
            {
                db.ClinicalNotes.AddRange(
                    new ClinicalNote
                    {
                        ClinicId = clinic.Id,
                        PatientId = patient.Id,
                        AuthorId = dietitian.Id,
                        NoteType = "nutrition",
                        Title = "Initial nutrition review",
                        Content = "Reviewed profile, calorie target, and first two weeks of logs. Patient is ready for a structured meal plan.",
                        IsSharedWithPatient = false,
                        CreatedAt = DateTime.UtcNow.AddDays(-8)
                    },
                    new ClinicalNote
                    {
                        ClinicId = clinic.Id,
                        PatientId = patient.Id,
                        AuthorId = dietitian.Id,
                        NoteType = "progress",
                        Title = "Progress check",
                        Content = "Hydration and adherence improved this week. Continue current plan and reassess after next appointment.",
                        IsSharedWithPatient = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-2)
                    });
            }

            if (db.Appointments.Count(a => a.ClinicId == clinic.Id && a.PatientId == patient.Id) < 2)
            {
                db.Appointments.AddRange(
                    new Appointment
                    {
                        ClinicId = clinic.Id,
                        PatientId = patient.Id,
                        DietitianId = dietitian.Id,
                        StartsAt = DateTime.UtcNow.AddDays(2).Date.AddHours(10),
                        DurationMinutes = 45,
                        Status = "scheduled",
                        VisitType = "followUp",
                        Location = "Clinic room 2",
                        Notes = "Review adherence and adjust meal plan.",
                        CreatedAt = DateTime.UtcNow.AddDays(-4)
                    },
                    new Appointment
                    {
                        ClinicId = clinic.Id,
                        PatientId = patient.Id,
                        DietitianId = dietitian.Id,
                        StartsAt = DateTime.UtcNow.AddDays(-7).Date.AddHours(12),
                        DurationMinutes = 30,
                        Status = "completed",
                        VisitType = "initial",
                        Location = "Telehealth",
                        Notes = "Completed onboarding consultation.",
                        CreatedAt = DateTime.UtcNow.AddDays(-12)
                    });
            }

            await db.SaveChangesAsync();
        }

        private static async Task SeedHamzaDataAsync(IServiceProvider services)
        {
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var db = services.GetRequiredService<ApplicationDbContext>();

            var user = await userManager.FindByEmailAsync("omargaga238@gmail.com");
            if (user == null) return;

            var userId = user.Id;
            var rng = new Random(42);
            var startDate = DateTime.Today.AddDays(-180);

            try
            {
                // 1. WeightLogs — 26 entries from 6 months ago, 98→84 kg
                if (db.WeightLogs.Count(w => w.UserId == userId) < 20)
                {
                    db.WeightLogs.RemoveRange(db.WeightLogs.Where(w => w.UserId == userId));
                    await db.SaveChangesAsync();

                    var weightLogs = new List<WeightLog>();
                    for (int i = 0; i < 26; i++)
                    {
                        var date = startDate.AddDays((int)(i * 180.0 / 25));
                        double trend = 14.0 * i / 25.0;
                        double fluctuation = (rng.NextDouble() - 0.5) * 1.5;
                        weightLogs.Add(new WeightLog
                        {
                            UserId = userId,
                            Date = date,
                            Weight = Math.Round(98.0 - trend + fluctuation, 1)
                        });
                    }
                    await db.WeightLogs.AddRangeAsync(weightLogs);
                    await db.SaveChangesAsync();
                    Console.WriteLine("[Seed] WeightLogs seeded for Hamza.");
                }

                // 2. DayLogs — 180 carnivore entries
                if (db.DayLogs.Count(d => d.UserId == userId) < 100)
                {
                    db.DayLogs.RemoveRange(db.DayLogs.Where(d => d.UserId == userId));
                    await db.SaveChangesAsync();

                    var moods = new[] { "postWorkout", "great", "needEnergy" };
                    var dayLogs = Enumerable.Range(0, 180).Select(i => new DayLog
                    {
                        UserId = userId,
                        Date = startDate.AddDays(i),
                        CaloriesConsumed = 2100 + rng.Next(401),
                        Protein = 160 + rng.Next(26),
                        Fats = 130 + rng.Next(26),
                        Carbs = 10 + rng.Next(16),
                        Mood = moods[i % 3],
                        Adherent = rng.NextDouble() < 0.94,
                        CaloriesBurned = 400 + rng.Next(201)
                    }).ToList();
                    await db.DayLogs.AddRangeAsync(dayLogs);
                    await db.SaveChangesAsync();
                    Console.WriteLine("[Seed] DayLogs seeded for Hamza.");
                }

                // 3. WaterLogs — 180 entries, 7–9 glasses
                if (db.WaterLogs.Count(w => w.UserId == userId) < 100)
                {
                    db.WaterLogs.RemoveRange(db.WaterLogs.Where(w => w.UserId == userId));
                    await db.SaveChangesAsync();

                    var waterLogs = Enumerable.Range(0, 180).Select(i => new WaterLog
                    {
                        UserId = userId,
                        Date = startDate.AddDays(i),
                        GlassesCount = 7 + rng.Next(3)
                    }).ToList();
                    await db.WaterLogs.AddRangeAsync(waterLogs);
                    await db.SaveChangesAsync();
                    Console.WriteLine("[Seed] WaterLogs seeded for Hamza.");
                }

                // 4. MealPlans — 2 AI carnivore 7-day plans
                if (db.MealPlans.Count(m => m.UserId == userId && m.PlanType == "ai") < 2)
                {
                    static string BuildCarnivorePlan(int offset)
                    {
                        var breakfasts = new[] { "بيض مقلي بالزبدة", "كبدة بقري مقلية", "بيض مسلوق مع لحم مفروم", "ستيك ريبي مع بيض", "بيض مقلي مع سجق لحم", "سمك سلمون بالزبدة", "دجاج مشوي بالأعشاب" };
                        var lunches   = new[] { "ستيك لحم بقري", "سمك سلمون مشوي", "دجاج مشوي", "كبد دجاج بالزبدة", "لحم غنم مشوي", "لحم بقري مفروم", "سمك مشوي بالليمون" };
                        var dinners   = new[] { "دجاج بالزبدة", "ستيك ريبي", "لحم بقري مشوي", "سمك تونا", "صدر دجاج مشوي", "لحم مفروم بالزبدة", "كبدة بقري مقلية" };

                        var sb = new System.Text.StringBuilder("{\"days\":[");
                        for (int d = 0; d < 7; d++)
                        {
                            if (d > 0) sb.Append(',');
                            int idx = (d + offset) % 7;
                            sb.Append($"{{\"day\":{d + 1},\"meals\":[");
                            sb.Append($"{{\"nameAr\":\"{breakfasts[idx]}\",\"type\":\"breakfast\",\"calories\":700,\"protein\":55,\"fats\":52,\"carbs\":0}},");
                            sb.Append($"{{\"nameAr\":\"{lunches[idx]}\",\"type\":\"lunch\",\"calories\":900,\"protein\":75,\"fats\":68,\"carbs\":0}},");
                            sb.Append($"{{\"nameAr\":\"{dinners[idx]}\",\"type\":\"dinner\",\"calories\":740,\"protein\":65,\"fats\":56,\"carbs\":0}}");
                            sb.Append($"],\"totalCalories\":2340}}");
                        }
                        sb.Append("]}");
                        return sb.ToString();
                    }

                    await db.MealPlans.AddRangeAsync(
                        new MealPlan
                        {
                            UserId = userId,
                            PlanType = "ai",
                            Title = "خطة الكارنيفور - الأسبوع الأول",
                            CalorieTarget = 2340,
                            DietType = "carnivore",
                            PlanJson = BuildCarnivorePlan(0),
                            CreatedAt = DateTime.UtcNow.AddDays(-30)
                        },
                        new MealPlan
                        {
                            UserId = userId,
                            PlanType = "ai",
                            Title = "خطة الكارنيفور - الأسبوع الثاني",
                            CalorieTarget = 2340,
                            DietType = "carnivore",
                            PlanJson = BuildCarnivorePlan(3),
                            CreatedAt = DateTime.UtcNow.AddDays(-15)
                        }
                    );
                    await db.SaveChangesAsync();
                    Console.WriteLine("[Seed] MealPlans seeded for Hamza.");
                }

                // 5. UserChallenges — carnivore + keto, 30/30 days completed
                if (!db.UserChallenges.Any(uc => uc.UserId == userId && uc.CurrentDay >= 30))
                {
                    var challengeStart = DateTime.Today.AddDays(-30);
                    var completedDaysJson = JsonSerializer.Serialize(
                        Enumerable.Range(0, 30)
                            .Select(d => challengeStart.AddDays(d).ToString("yyyy-MM-dd"))
                            .ToList());

                    foreach (var dietType in new[] { "carnivore", "keto" })
                    {
                        var challenge = db.Challenges.FirstOrDefault(c =>
                            c.DietType == dietType || c.Slug.Contains(dietType));
                        if (challenge == null) continue;
                        if (db.UserChallenges.Any(uc => uc.UserId == userId && uc.ChallengeId == challenge.Id)) continue;

                        db.UserChallenges.Add(new UserChallenge
                        {
                            UserId = userId,
                            ChallengeId = challenge.Id,
                            CurrentDay = 30,
                            Streak = 28,
                            StartDate = challengeStart,
                            CompletedDaysJson = completedDaysJson,
                            LastCheckIn = DateTime.Today
                        });
                    }
                    await db.SaveChangesAsync();
                    Console.WriteLine("[Seed] UserChallenges seeded for Hamza.");
                }

                // 6. UserAchievements
                foreach (var key in new[] { "streak_7", "streak_30", "nutrition_7", "hydration_7", "explorer_diet", "mood_tracker" })
                {
                    if (!db.UserAchievements.Any(a => a.UserId == userId && a.AchievementKey == key))
                    {
                        db.UserAchievements.Add(new UserAchievement
                        {
                            UserId = userId,
                            AchievementKey = key,
                            UnlockedAt = DateTime.UtcNow.AddDays(-rng.Next(1, 150))
                        });
                    }
                }
                await db.SaveChangesAsync();
                Console.WriteLine("[Seed] UserAchievements seeded for Hamza.");

                // 7. HealthProfile
                if (!db.HealthProfiles.Any(h => h.UserId == userId))
                {
                    db.HealthProfiles.Add(new HealthProfile
                    {
                        UserId = userId,
                        Age = 27,
                        Gender = "male",
                        Height = 178,
                        Weight = 84,
                        Goal = "loseWeight",
                        ActivityLevel = "veryActive",
                        DietSlug = "carnivore",
                        CalorieTarget = 2340,
                        WaterGoal = 8,
                        CookingStyle = "moderate",
                        Budget = "medium",
                        UpdatedAt = DateTime.UtcNow
                    });
                    await db.SaveChangesAsync();
                    Console.WriteLine("[Seed] HealthProfile seeded for Hamza.");
                }

                // 8. BodyProgressEntries — 6 monthly entries, 98→84 kg
                if (db.BodyProgressEntries.Count(b => b.UserId == userId) < 5)
                {
                    db.BodyProgressEntries.RemoveRange(db.BodyProgressEntries.Where(b => b.UserId == userId));
                    await db.SaveChangesAsync();

                    var progressNotes = new[] { "بداية الرحلة", "تحسن ملحوظ", "استمرار الانخفاض", "نتائج ممتازة", "اقتراب من الهدف", "تقدم رائع" };
                    var bodyEntries = Enumerable.Range(0, 6).Select(i => new BodyProgress
                    {
                        UserId = userId,
                        Date = DateTime.UtcNow.AddMonths(-i).Date,
                        Weight = Math.Round(98.0 - (5 - i) * 2.3, 1),
                        Waist  = Math.Round(102.0 - (5 - i) * 1.5, 1),
                        Hips   = Math.Round(108.0 - (5 - i) * 1.0, 1),
                        Chest  = Math.Round(105.0 - (5 - i) * 0.8, 1),
                        Arms   = Math.Round(38.0 - (5 - i) * 0.3, 1),
                        Notes  = progressNotes[5 - i],
                        CreatedAt = DateTime.UtcNow.AddMonths(-i)
                    }).ToList();

                    await db.BodyProgressEntries.AddRangeAsync(bodyEntries);
                    await db.SaveChangesAsync();
                    Console.WriteLine("[Seed] BodyProgressEntries seeded for Hamza.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Seed] Error seeding Hamza data: {ex.Message}");
            }
        }
    }
}
