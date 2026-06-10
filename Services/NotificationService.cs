using MoodBite.Data;
using MoodBite.Models;
using Microsoft.EntityFrameworkCore;

namespace MoodBite.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _db;

        public NotificationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _db.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task CreateDefaultNotificationsAsync(string userId)
        {
            var notifications = new List<Notification>
            {
                new() {
                    UserId = userId,
                    TitleAr = "ابدأ يومك بفطور صحي",
                    TitleEn = "Start Your Day with a Healthy Breakfast",
                    MessageAr = "لا تتخطى وجبة الفطور — إنها أهم وجبة في يومك!",
                    MessageEn = "Don't skip breakfast — it's the most important meal of the day!",
                    Type = "meal",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-30)
                },
                new() {
                    UserId = userId,
                    TitleAr = "تذكير بالتمرين",
                    TitleEn = "Workout Reminder",
                    MessageAr = "حان وقت تمرينك اليومي! لا تنسَ الإحماء.",
                    MessageEn = "Time for your daily workout! Don't forget to warm up.",
                    Type = "workout",
                    CreatedAt = DateTime.UtcNow.AddHours(-2)
                },
                new() {
                    UserId = userId,
                    TitleAr = "اشرب الماء! 💧",
                    TitleEn = "Stay Hydrated! 💧",
                    MessageAr = "لم تشرب كفايتك من الماء اليوم. هدفك 8 أكواب يومياً.",
                    MessageEn = "You haven't had enough water today. Aim for 8 glasses daily.",
                    Type = "hydration",
                    CreatedAt = DateTime.UtcNow.AddHours(-4)
                },
                new() {
                    UserId = userId,
                    TitleAr = "تذكير الماء الصباحي ☀️",
                    TitleEn = "Morning Hydration Reminder ☀️",
                    MessageAr = "ابدأ يومك بكأس ماء كبير. يُنشط الجهاز الهضمي ويُحسّن التركيز.",
                    MessageEn = "Start your day with a big glass of water. It boosts digestion and focus.",
                    Type = "hydration",
                    CreatedAt = DateTime.UtcNow.AddHours(-6)
                },
                new() {
                    UserId = userId,
                    TitleAr = "منتصف النهار — تذكير الماء 💧",
                    TitleEn = "Midday Hydration Check 💧",
                    MessageAr = "هل شربت 4 أكواب حتى الآن؟ الترطيب الجيد يحسّن أداءك.",
                    MessageEn = "Have you had 4 glasses so far? Good hydration improves your performance.",
                    Type = "hydration",
                    CreatedAt = DateTime.UtcNow.AddHours(-8)
                },
                new() {
                    UserId = userId,
                    TitleAr = "تذكير ماء المساء 🌙",
                    TitleEn = "Evening Water Reminder 🌙",
                    MessageAr = "لا تنسَ شرب الماء قبل النوم — يساعد على التعافي أثناء النوم.",
                    MessageEn = "Don't forget water before bed — it helps recovery while you sleep.",
                    Type = "hydration",
                    CreatedAt = DateTime.UtcNow.AddHours(-10)
                },
                new() {
                    UserId = userId,
                    TitleAr = "تقريرك الأسبوعي جاهز",
                    TitleEn = "Your Weekly Report is Ready",
                    MessageAr = "اطلع على تقريرك الأسبوعي لمعرفة تقدمك الصحي.",
                    MessageEn = "Check your weekly report to see your health progress.",
                    Type = "report",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new() {
                    UserId = userId,
                    TitleAr = "وجبة ما بعد التمرين",
                    TitleEn = "Post-Workout Nutrition Alert",
                    MessageAr = "تذكر تناول وجبة غنية بالبروتين بعد التمرين خلال 30 دقيقة.",
                    MessageEn = "Remember to eat a protein-rich meal within 30 minutes after workout.",
                    Type = "meal",
                    CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(-3)
                },
                new() {
                    UserId = userId,
                    TitleAr = "تحضير وجبات الغد",
                    TitleEn = "Evening Meal Prep Tip",
                    MessageAr = "جهّز وجباتك مسبقاً لليوم القادم لتوفير الوقت والالتزام بخطتك.",
                    MessageEn = "Prep your meals for tomorrow to save time and stay on track.",
                    Type = "meal",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new() {
                    UserId = userId,
                    TitleAr = "تذكير بتسجيل وجباتك",
                    TitleEn = "Meal Logging Reminder",
                    MessageAr = "لم تسجّل وجباتك اليوم. سجّلها الآن لتبقى في المسار الصحيح.",
                    MessageEn = "You haven't logged your meals today. Log them now to stay on track.",
                    Type = "reminder",
                    CreatedAt = DateTime.UtcNow.AddDays(-2).AddHours(-5)
                },
                new() {
                    UserId = userId,
                    TitleAr = "تنبيه تعارض النظام الغذائي",
                    TitleEn = "Diet Conflict Warning",
                    MessageAr = "يبدو أن بعض وجباتك لا تتوافق مع نظامك الغذائي المختار.",
                    MessageEn = "Some of your meals seem to conflict with your chosen diet plan.",
                    Type = "reminder",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new() {
                    UserId = userId,
                    TitleAr = "حان وقت قياساتك الأسبوعية 📏",
                    TitleEn = "Time to Log Your Weekly Measurements! 📏",
                    MessageAr = "كل أسبوع، سجّل قياساتك الجسدية لتتبع تقدمك ورؤية نتائج جهودك.",
                    MessageEn = "Every week, log your body measurements to track your progress and see the results of your efforts.",
                    Type = "reminder",
                    CreatedAt = DateTime.UtcNow.AddDays(-3).AddHours(-2)
                }
            };

            await _db.Notifications.AddRangeAsync(notifications);
            await _db.SaveChangesAsync();
        }

        public async Task EnsureSundayProgressReminderAsync(string userId)
        {
            if (DateTime.Today.DayOfWeek != DayOfWeek.Sunday) return;

            var alreadySent = await _db.Notifications.AnyAsync(n =>
                n.UserId == userId &&
                n.Type == "progress_reminder" &&
                n.CreatedAt.Date == DateTime.Today);

            if (alreadySent) return;

            _db.Notifications.Add(new Notification
            {
                UserId = userId,
                TitleAr = "حان وقت قياساتك الأسبوعية 📏",
                TitleEn = "Time to Log Your Weekly Measurements! 📏",
                MessageAr = "إنه يوم الأحد! سجّل قياساتك الجسدية اليوم لتتابع تقدمك بشكل أسبوعي.",
                MessageEn = "It's Sunday! Log your body measurements today to track your weekly progress.",
                Type = "progress_reminder",
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
    }
}
