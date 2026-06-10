using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MoodBite.Models;

namespace MoodBite.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<HealthProfile> HealthProfiles { get; set; }
        public DbSet<DayLog> DayLogs { get; set; }
        public DbSet<WeightLog> WeightLogs { get; set; }
        public DbSet<WaterLog> WaterLogs { get; set; }
        public DbSet<Diet> Diets { get; set; }
        public DbSet<CommunityRecipe> CommunityRecipes { get; set; }
        public DbSet<RecipeLike> RecipeLikes { get; set; }
        public DbSet<RecipeSave> RecipeSaves { get; set; }
        public DbSet<Challenge> Challenges { get; set; }
        public DbSet<UserChallenge> UserChallenges { get; set; }
        public DbSet<WorkoutPlan> WorkoutPlans { get; set; }
        public DbSet<MealPlan> MealPlans { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<BuddyRequest> BuddyRequests { get; set; }
        public DbSet<Restaurant> Restaurants { get; set; }
        public DbSet<UserAchievement> UserAchievements { get; set; }
        public DbSet<FoodScan> FoodScans { get; set; }
        public DbSet<BodyProgress> BodyProgressEntries { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ApplicationUser relationships
            builder.Entity<ApplicationUser>()
                .HasMany(u => u.SentBuddyRequests)
                .WithOne(b => b.Sender)
                .HasForeignKey(b => b.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ApplicationUser>()
                .HasMany(u => u.ReceivedBuddyRequests)
                .WithOne(b => b.Receiver)
                .HasForeignKey(b => b.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // HealthProfile: one-to-one
            builder.Entity<HealthProfile>()
                .HasOne(h => h.User)
                .WithOne(u => u.HealthProfile)
                .HasForeignKey<HealthProfile>(h => h.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // DayLog
            builder.Entity<DayLog>()
                .HasOne(d => d.User)
                .WithMany(u => u.DayLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // WeightLog
            builder.Entity<WeightLog>()
                .HasOne(w => w.User)
                .WithMany(u => u.WeightLogs)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // FoodScan
            builder.Entity<FoodScan>()
                .HasOne(f => f.User)
                .WithMany(u => u.FoodScans)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserAchievement
            builder.Entity<UserAchievement>()
                .HasOne(a => a.User)
                .WithMany(u => u.UserAchievements)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserAchievement>()
                .HasIndex(a => new { a.UserId, a.AchievementKey })
                .IsUnique();

            // WaterLog
            builder.Entity<WaterLog>()
                .HasOne(w => w.User)
                .WithMany(u => u.WaterLogs)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Notification
            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // WorkoutPlan
            builder.Entity<WorkoutPlan>()
                .HasOne(w => w.User)
                .WithMany(u => u.WorkoutPlans)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // MealPlan
            builder.Entity<MealPlan>()
                .HasOne(m => m.User)
                .WithMany(u => u.MealPlans)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserChallenge
            builder.Entity<UserChallenge>()
                .HasOne(uc => uc.User)
                .WithMany(u => u.UserChallenges)
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserChallenge>()
                .HasOne(uc => uc.Challenge)
                .WithMany(c => c.UserChallenges)
                .HasForeignKey(uc => uc.ChallengeId)
                .OnDelete(DeleteBehavior.Cascade);

            // CommunityRecipe
            builder.Entity<CommunityRecipe>()
                .HasOne(r => r.User)
                .WithMany(u => u.CommunityRecipes)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // RecipeLike
            builder.Entity<RecipeLike>()
                .HasOne(rl => rl.Recipe)
                .WithMany(r => r.RecipeLikes)
                .HasForeignKey(rl => rl.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RecipeLike>()
                .HasOne(rl => rl.User)
                .WithMany()
                .HasForeignKey(rl => rl.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // RecipeSave
            builder.Entity<RecipeSave>()
                .HasOne(rs => rs.Recipe)
                .WithMany(r => r.RecipeSaves)
                .HasForeignKey(rs => rs.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RecipeSave>()
                .HasOne(rs => rs.User)
                .WithMany()
                .HasForeignKey(rs => rs.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // BodyProgress
            builder.Entity<BodyProgress>()
                .HasOne(b => b.User)
                .WithMany(u => u.BodyProgressEntries)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique index on Diet.Slug
            builder.Entity<Diet>()
                .HasIndex(d => d.Slug)
                .IsUnique();
        }
    }
}
