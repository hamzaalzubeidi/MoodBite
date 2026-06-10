using MoodBite.Data;
using MoodBite.Models;
using Microsoft.EntityFrameworkCore;

namespace MoodBite.Services
{
    public class AchievementDefinition
    {
        public string Key { get; init; } = "";
        public string NameAr { get; init; } = "";
        public string NameEn { get; init; } = "";
        public string DescAr { get; init; } = "";
        public string DescEn { get; init; } = "";
        public string Emoji { get; init; } = "🏅";
        public string Category { get; init; } = "";
        public int Points { get; init; }
        public int RequiredCount { get; init; }
        public string Gradient { get; init; } = "linear-gradient(135deg,#059669,#0d9488)";
    }

    public class AchievementViewModel
    {
        public AchievementDefinition Definition { get; set; } = null!;
        public bool IsUnlocked { get; set; }
        public DateTime? UnlockedAt { get; set; }
        public int Progress { get; set; }
    }

    public class AchievementService
    {
        private readonly ApplicationDbContext _db;

        public static readonly List<AchievementDefinition> All = new()
        {
            // ── Streak ──────────────────────────────────────────────────────────
            new() { Key="first-log",       Category="streak",    Points=5,   RequiredCount=1,  Emoji="🌱", Gradient="linear-gradient(135deg,#10b981,#059669)",
                    NameAr="الخطوة الأولى",        NameEn="First Step",         DescAr="سجّل يومك الأول",              DescEn="Log your first day" },
            new() { Key="week-warrior",    Category="streak",    Points=20,  RequiredCount=7,  Emoji="⚔️", Gradient="linear-gradient(135deg,#3b82f6,#6366f1)",
                    NameAr="محارب الأسبوع",         NameEn="Week Warrior",       DescAr="7 أيام متتالية من التسجيل",    DescEn="7 consecutive logged days" },
            new() { Key="fortnight",       Category="streak",    Points=35,  RequiredCount=14, Emoji="💪", Gradient="linear-gradient(135deg,#8b5cf6,#6366f1)",
                    NameAr="أسبوعان من الثبات",     NameEn="Two Weeks Strong",   DescAr="14 يومًا متتالية",              DescEn="14 consecutive logged days" },
            new() { Key="month-master",    Category="streak",    Points=100, RequiredCount=30, Emoji="👑", Gradient="linear-gradient(135deg,#f59e0b,#ef4444)",
                    NameAr="سيد الشهر",             NameEn="Month Master",       DescAr="30 يومًا متتالية",              DescEn="30 consecutive logged days" },

            // ── Nutrition ───────────────────────────────────────────────────────
            new() { Key="calorie-champion",Category="nutrition", Points=25,  RequiredCount=7,  Emoji="🎯", Gradient="linear-gradient(135deg,#f59e0b,#f97316)",
                    NameAr="بطل السعرات",           NameEn="Calorie Champion",   DescAr="7 أيام ضمن الهدف السعري",       DescEn="Stay within calorie target 7 days" },
            new() { Key="perfect-week",    Category="nutrition", Points=40,  RequiredCount=7,  Emoji="✨", Gradient="linear-gradient(135deg,#fbbf24,#f59e0b)",
                    NameAr="أسبوع مثالي",           NameEn="Perfect Week",       DescAr="7 أيام متتالية بالتزام كامل",   DescEn="7 consecutive fully adherent days" },
            new() { Key="macro-master",    Category="nutrition", Points=20,  RequiredCount=10, Emoji="🔬", Gradient="linear-gradient(135deg,#06b6d4,#3b82f6)",
                    NameAr="خبير المغذيات",          NameEn="Macro Master",       DescAr="سجّل المغذيات 10 مرات",         DescEn="Log macros 10 times" },

            // ── Hydration ───────────────────────────────────────────────────────
            new() { Key="hydration-starter",Category="hydration",Points=5,  RequiredCount=1,  Emoji="💧", Gradient="linear-gradient(135deg,#06b6d4,#0891b2)",
                    NameAr="مبادرة الماء",           NameEn="Hydration Starter",  DescAr="حقق هدف الماء اليومي مرة",      DescEn="Reach your daily water goal once" },
            new() { Key="hydration-hero",  Category="hydration", Points=25,  RequiredCount=7,  Emoji="🌊", Gradient="linear-gradient(135deg,#0ea5e9,#06b6d4)",
                    NameAr="بطل الترطيب",           NameEn="Hydration Hero",      DescAr="حقق هدف الماء 7 أيام",          DescEn="Reach water goal 7 days" },
            new() { Key="hydration-legend",Category="hydration", Points=50,  RequiredCount=14, Emoji="🏊", Gradient="linear-gradient(135deg,#3b82f6,#0ea5e9)",
                    NameAr="أسطورة الترطيب",        NameEn="Hydration Legend",    DescAr="حقق هدف الماء 14 يومًا",        DescEn="Reach water goal 14 days" },

            // ── Mood ────────────────────────────────────────────────────────────
            new() { Key="mood-logger",     Category="mood",      Points=10,  RequiredCount=7,  Emoji="😊", Gradient="linear-gradient(135deg,#ec4899,#f43f5e)",
                    NameAr="متتبع المزاج",           NameEn="Mood Logger",         DescAr="سجّل مزاجك 7 مرات",             DescEn="Log your mood 7 times" },
            new() { Key="mood-expert",     Category="mood",      Points=30,  RequiredCount=30, Emoji="🧠", Gradient="linear-gradient(135deg,#a855f7,#ec4899)",
                    NameAr="خبير المزاج",            NameEn="Mood Expert",         DescAr="سجّل مزاجك 30 مرة",             DescEn="Log your mood 30 times" },

            // ── Explorer ────────────────────────────────────────────────────────
            new() { Key="planner",         Category="explorer",  Points=5,   RequiredCount=1,  Emoji="📅", Gradient="linear-gradient(135deg,#059669,#10b981)",
                    NameAr="المخطط",                NameEn="The Planner",         DescAr="أنشئ خطة وجبات",                DescEn="Create a meal plan" },
            new() { Key="challenger",      Category="explorer",  Points=10,  RequiredCount=1,  Emoji="🏆", Gradient="linear-gradient(135deg,#f59e0b,#fbbf24)",
                    NameAr="المتحدي",               NameEn="The Challenger",      DescAr="انضم لتحدي 30 يومًا",           DescEn="Join a 30-day challenge" },
            new() { Key="community-member",Category="explorer",  Points=15,  RequiredCount=1,  Emoji="👥", Gradient="linear-gradient(135deg,#0d9488,#059669)",
                    NameAr="عضو المجتمع",           NameEn="Community Member",    DescAr="شارك وصفة في المجتمع",          DescEn="Share a recipe in the community" },
        };

        public AchievementService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<string>> CheckAndUnlockAsync(string userId)
        {
            var unlocked = await _db.UserAchievements
                .Where(a => a.UserId == userId)
                .Select(a => a.AchievementKey)
                .ToListAsync();

            var dayLogs       = await _db.DayLogs.Where(d => d.UserId == userId).OrderBy(d => d.Date).ToListAsync();
            var waterLogs     = await _db.WaterLogs.Where(w => w.UserId == userId).ToListAsync();
            var weightLogs    = await _db.WeightLogs.Where(w => w.UserId == userId).ToListAsync();
            var mealPlans     = await _db.MealPlans.Where(m => m.UserId == userId).CountAsync();
            var challenges    = await _db.UserChallenges.Where(c => c.UserId == userId).CountAsync();
            var recipes       = await _db.CommunityRecipes.Where(r => r.UserId == userId).CountAsync();
            var waterGoal     = (await _db.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == userId))?.WaterGoal ?? 8;

            var streak      = ComputeMaxStreak(dayLogs);
            var adherentDays = dayLogs.Count(l => l.Adherent);
            var consecutiveAdherent = ComputeMaxConsecutiveAdherent(dayLogs);
            var moodDays    = dayLogs.Count(l => !string.IsNullOrEmpty(l.Mood));
            var macroDays   = dayLogs.Count(l => l.Protein > 0);
            var waterGoalDays = waterLogs.Count(w => w.GlassesCount >= waterGoal);

            var conditions = new Dictionary<string, int>
            {
                ["first-log"]          = dayLogs.Count,
                ["week-warrior"]       = streak,
                ["fortnight"]          = streak,
                ["month-master"]       = streak,
                ["calorie-champion"]   = adherentDays,
                ["perfect-week"]       = consecutiveAdherent,
                ["macro-master"]       = macroDays,
                ["hydration-starter"]  = waterGoalDays,
                ["hydration-hero"]     = waterGoalDays,
                ["hydration-legend"]   = waterGoalDays,
                ["mood-logger"]        = moodDays,
                ["mood-expert"]        = moodDays,
                ["planner"]            = mealPlans,
                ["challenger"]         = challenges,
                ["community-member"]   = recipes,
            };

            var newlyUnlocked = new List<string>();
            var toAdd = new List<UserAchievement>();

            foreach (var def in All)
            {
                if (unlocked.Contains(def.Key)) continue;
                if (conditions.TryGetValue(def.Key, out var progress) && progress >= def.RequiredCount)
                {
                    toAdd.Add(new UserAchievement { UserId = userId, AchievementKey = def.Key, UnlockedAt = DateTime.UtcNow });
                    newlyUnlocked.Add(def.Key);
                }
            }

            if (toAdd.Count > 0)
            {
                _db.UserAchievements.AddRange(toAdd);
                await _db.SaveChangesAsync();
            }

            return newlyUnlocked;
        }

        public async Task<(List<AchievementViewModel> Achievements, int TotalPoints, int PossiblePoints)> GetPageDataAsync(string userId)
        {
            var unlocked = await _db.UserAchievements
                .Where(a => a.UserId == userId)
                .ToListAsync();

            var unlockedMap = unlocked.ToDictionary(a => a.AchievementKey, a => a.UnlockedAt);

            var dayLogs    = await _db.DayLogs.Where(d => d.UserId == userId).OrderBy(d => d.Date).ToListAsync();
            var waterLogs  = await _db.WaterLogs.Where(w => w.UserId == userId).ToListAsync();
            var waterGoal  = (await _db.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == userId))?.WaterGoal ?? 8;
            var mealPlans  = await _db.MealPlans.Where(m => m.UserId == userId).CountAsync();
            var challenges = await _db.UserChallenges.Where(c => c.UserId == userId).CountAsync();
            var recipes    = await _db.CommunityRecipes.Where(r => r.UserId == userId).CountAsync();

            var streak             = ComputeMaxStreak(dayLogs);
            var adherentDays       = dayLogs.Count(l => l.Adherent);
            var consecutiveAdherent= ComputeMaxConsecutiveAdherent(dayLogs);
            var moodDays           = dayLogs.Count(l => !string.IsNullOrEmpty(l.Mood));
            var macroDays          = dayLogs.Count(l => l.Protein > 0);
            var waterGoalDays      = waterLogs.Count(w => w.GlassesCount >= waterGoal);

            var progress = new Dictionary<string, int>
            {
                ["first-log"]          = dayLogs.Count,
                ["week-warrior"]       = streak,
                ["fortnight"]          = streak,
                ["month-master"]       = streak,
                ["calorie-champion"]   = adherentDays,
                ["perfect-week"]       = consecutiveAdherent,
                ["macro-master"]       = macroDays,
                ["hydration-starter"]  = waterGoalDays,
                ["hydration-hero"]     = waterGoalDays,
                ["hydration-legend"]   = waterGoalDays,
                ["mood-logger"]        = moodDays,
                ["mood-expert"]        = moodDays,
                ["planner"]            = mealPlans,
                ["challenger"]         = challenges,
                ["community-member"]   = recipes,
            };

            var vms = All.Select(def => new AchievementViewModel
            {
                Definition  = def,
                IsUnlocked  = unlockedMap.ContainsKey(def.Key),
                UnlockedAt  = unlockedMap.TryGetValue(def.Key, out var dt) ? dt : null,
                Progress    = progress.TryGetValue(def.Key, out var p) ? Math.Min(p, def.RequiredCount) : 0,
            }).ToList();

            var totalPoints    = vms.Where(v => v.IsUnlocked).Sum(v => v.Definition.Points);
            var possiblePoints = All.Sum(d => d.Points);

            return (vms, totalPoints, possiblePoints);
        }

        public async Task<string?> GetTopBadgeEmojiAsync(string userId)
        {
            var keys = await _db.UserAchievements
                .Where(a => a.UserId == userId)
                .Select(a => a.AchievementKey)
                .ToListAsync();

            if (!keys.Any()) return null;

            return All
                .Where(d => keys.Contains(d.Key))
                .OrderByDescending(d => d.Points)
                .FirstOrDefault()?.Emoji;
        }

        public async Task<Dictionary<string, string?>> GetTopBadgesForUsersAsync(IEnumerable<string> userIds)
        {
            var ids = userIds.ToList();
            var rows = await _db.UserAchievements
                .Where(a => ids.Contains(a.UserId))
                .ToListAsync();

            return ids.ToDictionary(uid => uid, uid =>
            {
                var keys = rows.Where(r => r.UserId == uid).Select(r => r.AchievementKey).ToList();
                return All.Where(d => keys.Contains(d.Key))
                          .OrderByDescending(d => d.Points)
                          .FirstOrDefault()?.Emoji;
            });
        }

        private static int ComputeMaxStreak(List<DayLog> logs)
        {
            if (!logs.Any()) return 0;
            int max = 1, current = 1;
            for (int i = 1; i < logs.Count; i++)
            {
                if ((logs[i].Date.Date - logs[i - 1].Date.Date).Days == 1)
                    current++;
                else
                    current = 1;
                if (current > max) max = current;
            }
            return max;
        }

        private static int ComputeMaxConsecutiveAdherent(List<DayLog> logs)
        {
            if (!logs.Any()) return 0;
            int max = 0, current = 0;
            foreach (var log in logs)
            {
                if (log.Adherent) { current++; if (current > max) max = current; }
                else current = 0;
            }
            return max;
        }
    }
}
