using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MoodBite.Constants;
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
        public DbSet<Clinic> Clinics { get; set; }
        public DbSet<ClinicMember> ClinicMembers { get; set; }
        public DbSet<ClinicPatient> ClinicPatients { get; set; }
        public DbSet<ClinicInvitation> ClinicInvitations { get; set; }
        public DbSet<ClinicalNote> ClinicalNotes { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

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

            // Clinic tenant model
            builder.Entity<Clinic>(entity =>
            {
                entity.HasIndex(c => c.Slug)
                    .IsUnique();

                entity.HasIndex(c => c.IsActive);

                entity.Property(c => c.Name)
                    .HasMaxLength(160)
                    .IsRequired();

                entity.Property(c => c.Slug)
                    .HasMaxLength(80)
                    .IsRequired();

                entity.Property(c => c.LegalName)
                    .HasMaxLength(160);

                entity.Property(c => c.Email)
                    .HasMaxLength(256);

                entity.Property(c => c.Phone)
                    .HasMaxLength(40);

                entity.Property(c => c.Country)
                    .HasMaxLength(120);

                entity.Property(c => c.City)
                    .HasMaxLength(120);

                entity.Property(c => c.Address)
                    .HasMaxLength(300);
            });

            builder.Entity<ClinicMember>(entity =>
            {
                entity.ToTable("ClinicMembers", table =>
                    table.HasCheckConstraint(
                        "CK_ClinicMembers_Role",
                        $"[Role] IN ('{ApplicationRoles.ClinicOwner}', '{ApplicationRoles.Dietitian}', '{ApplicationRoles.ClinicStaff}')"));

                entity.HasIndex(m => new { m.ClinicId, m.UserId })
                    .IsUnique();

                entity.HasIndex(m => m.UserId);
                entity.HasIndex(m => new { m.ClinicId, m.Role, m.IsActive });

                entity.Property(m => m.Role)
                    .HasMaxLength(64)
                    .IsRequired();

                entity.HasOne(m => m.Clinic)
                    .WithMany(c => c.Members)
                    .HasForeignKey(m => m.ClinicId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.User)
                    .WithMany(u => u.ClinicMemberships)
                    .HasForeignKey(m => m.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.InvitedBy)
                    .WithMany(u => u.InvitedClinicMembers)
                    .HasForeignKey(m => m.InvitedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ClinicPatient>(entity =>
            {
                entity.ToTable("ClinicPatients", table =>
                    table.HasCheckConstraint(
                        "CK_ClinicPatients_Status",
                        "[Status] IN ('pending', 'active', 'archived', 'discharged')"));

                entity.HasIndex(p => new { p.ClinicId, p.PatientId })
                    .IsUnique();

                entity.HasIndex(p => p.PatientId);
                entity.HasIndex(p => p.PrimaryDietitianId);
                entity.HasIndex(p => new { p.ClinicId, p.Status });

                entity.Property(p => p.Status)
                    .HasMaxLength(40)
                    .IsRequired();

                entity.Property(p => p.InternalNotes)
                    .HasMaxLength(500);

                entity.HasOne(p => p.Clinic)
                    .WithMany(c => c.Patients)
                    .HasForeignKey(p => p.ClinicId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Patient)
                    .WithMany(u => u.ClinicPatientLinks)
                    .HasForeignKey(p => p.PatientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.PrimaryDietitian)
                    .WithMany(u => u.AssignedClinicPatients)
                    .HasForeignKey(p => p.PrimaryDietitianId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ClinicInvitation>(entity =>
            {
                entity.ToTable("ClinicInvitations", table =>
                {
                    table.HasCheckConstraint(
                        "CK_ClinicInvitations_Type",
                        "[InvitationType] IN ('patient', 'staff')");
                    table.HasCheckConstraint(
                        "CK_ClinicInvitations_Status",
                        "[Status] IN ('pending', 'accepted', 'revoked', 'expired')");
                    table.HasCheckConstraint(
                        "CK_ClinicInvitations_TargetRole",
                        $"[TargetRole] IS NULL OR [TargetRole] IN ('{ApplicationRoles.ClinicOwner}', '{ApplicationRoles.Dietitian}', '{ApplicationRoles.ClinicStaff}')");
                });

                entity.HasIndex(i => i.TokenHash)
                    .IsUnique();

                entity.HasIndex(i => new { i.ClinicId, i.Email, i.Status });
                entity.HasIndex(i => i.InvitedByUserId);
                entity.HasIndex(i => i.AcceptedByUserId);
                entity.HasIndex(i => i.ExpiresAt);

                entity.Property(i => i.Email)
                    .HasMaxLength(256)
                    .IsRequired();

                entity.Property(i => i.TokenHash)
                    .HasMaxLength(256)
                    .IsRequired();

                entity.Property(i => i.InvitationType)
                    .HasMaxLength(40)
                    .IsRequired();

                entity.Property(i => i.TargetRole)
                    .HasMaxLength(64);

                entity.Property(i => i.Status)
                    .HasMaxLength(40)
                    .IsRequired();

                entity.HasOne(i => i.Clinic)
                    .WithMany(c => c.Invitations)
                    .HasForeignKey(i => i.ClinicId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(i => i.InvitedBy)
                    .WithMany(u => u.SentClinicInvitations)
                    .HasForeignKey(i => i.InvitedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(i => i.AcceptedBy)
                    .WithMany(u => u.AcceptedClinicInvitations)
                    .HasForeignKey(i => i.AcceptedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ClinicalNote>(entity =>
            {
                entity.HasIndex(n => new { n.ClinicId, n.PatientId, n.CreatedAt });
                entity.HasIndex(n => n.AuthorId);
                entity.HasIndex(n => new { n.ClinicId, n.NoteType, n.IsArchived });

                entity.Property(n => n.NoteType)
                    .HasMaxLength(40)
                    .IsRequired();

                entity.Property(n => n.Title)
                    .HasMaxLength(200);

                entity.Property(n => n.Content)
                    .HasMaxLength(4000)
                    .IsRequired();

                entity.HasOne(n => n.Clinic)
                    .WithMany(c => c.ClinicalNotes)
                    .HasForeignKey(n => n.ClinicId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(n => n.Patient)
                    .WithMany(u => u.PatientClinicalNotes)
                    .HasForeignKey(n => n.PatientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(n => n.Author)
                    .WithMany(u => u.AuthoredClinicalNotes)
                    .HasForeignKey(n => n.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            builder.Entity<AuditLog>(entity =>
            {
                entity.HasIndex(a => a.CreatedAtUtc);
                entity.HasIndex(a => a.ActorUserId);
                entity.HasIndex(a => a.ActorEmail);
                entity.HasIndex(a => new { a.ClinicId, a.CreatedAtUtc });
                entity.HasIndex(a => new { a.TargetEntityType, a.TargetEntityId });
                entity.HasIndex(a => new { a.Action, a.CreatedAtUtc });

                entity.Property(a => a.ActorEmail)
                    .HasMaxLength(256);

                entity.Property(a => a.ActorUserId)
                    .HasMaxLength(450);

                entity.Property(a => a.ActorRoles)
                    .HasMaxLength(256);

                entity.Property(a => a.TargetUserId)
                    .HasMaxLength(450);

                entity.Property(a => a.TargetEntityType)
                    .HasMaxLength(80)
                    .IsRequired();

                entity.Property(a => a.TargetEntityId)
                    .HasMaxLength(120);

                entity.Property(a => a.Action)
                    .HasMaxLength(120)
                    .IsRequired();

                entity.Property(a => a.Summary)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(a => a.IpAddress)
                    .HasMaxLength(64);

                entity.Property(a => a.UserAgent)
                    .HasMaxLength(500);

                entity.Property(a => a.MetadataJson)
                    .HasMaxLength(2000);
            });
            builder.Entity<Appointment>(entity =>
            {
                entity.ToTable("Appointments", table =>
                {
                    table.HasCheckConstraint(
                        "CK_Appointments_Status",
                        "[Status] IN ('scheduled', 'completed', 'cancelled', 'noShow')");
                    table.HasCheckConstraint(
                        "CK_Appointments_DurationMinutes",
                        "[DurationMinutes] BETWEEN 5 AND 480");
                });

                entity.HasIndex(a => new { a.ClinicId, a.StartsAt });
                entity.HasIndex(a => new { a.PatientId, a.StartsAt });
                entity.HasIndex(a => new { a.DietitianId, a.StartsAt });
                entity.HasIndex(a => new { a.ClinicId, a.Status });

                entity.Property(a => a.Status)
                    .HasMaxLength(40)
                    .IsRequired();

                entity.Property(a => a.VisitType)
                    .HasMaxLength(40)
                    .IsRequired();

                entity.Property(a => a.Location)
                    .HasMaxLength(200);

                entity.Property(a => a.Notes)
                    .HasMaxLength(1000);

                entity.HasOne(a => a.Clinic)
                    .WithMany(c => c.Appointments)
                    .HasForeignKey(a => a.ClinicId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Patient)
                    .WithMany(u => u.PatientAppointments)
                    .HasForeignKey(a => a.PatientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Dietitian)
                    .WithMany(u => u.DietitianAppointments)
                    .HasForeignKey(a => a.DietitianId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
