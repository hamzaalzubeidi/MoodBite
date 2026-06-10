using System.Data.Common;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoodBite.Constants;
using MoodBite.Data;
using MoodBite.Models;
using MoodBite.Services;
using MoodBite.ViewModels.Admin;

namespace MoodBite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = ApplicationRoles.Admin)]
    public class AdminClinicsController : Controller
    {
        private const int MaxSlugLength = 80;

        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly TranslationService _t;
        private readonly ILogger<AdminClinicsController> _logger;

        public AdminClinicsController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            TranslationService t,
            ILogger<AdminClinicsController> logger)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _t = t;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var model = new AdminClinicManagementViewModel();

            try
            {
                model.Clinics = await _db.Clinics.AsNoTracking()
                    .OrderBy(c => c.Name)
                    .Select(c => new AdminClinicListItem
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Slug = c.Slug,
                        City = c.City,
                        Country = c.Country,
                        IsActive = c.IsActive,
                        MemberCount = c.Members.Count,
                        PatientCount = c.Patients.Count
                    })
                    .ToListAsync(cancellationToken);
            }
            catch (DbException ex)
            {
                model.ClinicDataUnavailable = true;
                _logger.LogWarning(ex, "Admin clinic management data is unavailable. The clinic migration may be pending.");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminCreateClinicViewModel model, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["Error"] = _t.Get("clinic.validation.nameRequired");
                return RedirectToIndex();
            }

            try
            {
                var name = model.Name.Trim();
                var clinic = new MoodBite.Models.Clinic
                {
                    Name = name,
                    Slug = await GenerateUniqueSlugAsync(name, cancellationToken),
                    LegalName = Clean(model.LegalName),
                    Email = Clean(model.Email),
                    Phone = Clean(model.Phone),
                    Country = Clean(model.Country),
                    City = Clean(model.City),
                    Address = Clean(model.Address),
                    IsActive = true
                };

                _db.Clinics.Add(clinic);
                await _db.SaveChangesAsync(cancellationToken);

                TempData["Success"] = _t.Get("clinic.admin.created");
            }
            catch (DbException ex)
            {
                _logger.LogWarning(ex, "Unable to create clinic. The clinic migration may be pending.");
                TempData["Error"] = _t.Get("clinic.dataUnavailable");
            }

            return RedirectToIndex();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignOwner(AdminAssignClinicOwnerViewModel model, CancellationToken cancellationToken)
        {
            if (model.ClinicId <= 0 || string.IsNullOrWhiteSpace(model.Email))
            {
                TempData["Error"] = _t.Get("common.error");
                return RedirectToIndex();
            }

            try
            {
                var clinicExists = await _db.Clinics.AsNoTracking()
                    .AnyAsync(c => c.Id == model.ClinicId, cancellationToken);
                if (!clinicExists)
                {
                    TempData["Error"] = _t.Get("clinic.notFound");
                    return RedirectToIndex();
                }

                var user = await _userManager.FindByEmailAsync(model.Email.Trim());
                if (user == null)
                {
                    TempData["Error"] = _t.Get("clinic.userNotFound");
                    return RedirectToIndex();
                }

                var currentUserId = _userManager.GetUserId(User);
                var member = await _db.ClinicMembers
                    .FirstOrDefaultAsync(m => m.ClinicId == model.ClinicId && m.UserId == user.Id, cancellationToken);

                if (member == null)
                {
                    _db.ClinicMembers.Add(new ClinicMember
                    {
                        ClinicId = model.ClinicId,
                        UserId = user.Id,
                        Role = ApplicationRoles.ClinicOwner,
                        IsActive = true,
                        InvitedByUserId = currentUserId
                    });
                }
                else
                {
                    member.Role = ApplicationRoles.ClinicOwner;
                    member.IsActive = true;
                }

                await EnsureIdentityRoleAsync(user, ApplicationRoles.ClinicOwner);
                await _db.SaveChangesAsync(cancellationToken);

                TempData["Success"] = _t.Get("clinic.admin.ownerAssigned");
            }
            catch (DbException ex)
            {
                _logger.LogWarning(ex, "Unable to assign clinic owner. The clinic migration may be pending.");
                TempData["Error"] = _t.Get("clinic.dataUnavailable");
            }

            return RedirectToIndex();
        }

        private RedirectToActionResult RedirectToIndex() =>
            RedirectToAction(nameof(Index), "AdminClinics", new { area = "Admin" });

        private async Task<string> GenerateUniqueSlugAsync(string name, CancellationToken cancellationToken)
        {
            var baseSlug = ToSlug(name);
            var slug = baseSlug;
            var suffix = 2;

            while (await _db.Clinics.AsNoTracking().AnyAsync(c => c.Slug == slug, cancellationToken))
            {
                slug = BuildSlugWithSuffix(baseSlug, suffix++);
            }

            return slug;
        }

        private static string ToSlug(string value)
        {
            var builder = new StringBuilder();
            foreach (var ch in value.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(ch))
                {
                    builder.Append(ch);
                }
                else if (builder.Length > 0 && builder[^1] != '-')
                {
                    builder.Append('-');
                }
            }

            var slug = builder.ToString().Trim('-');
            if (slug.Length > MaxSlugLength)
            {
                slug = slug[..MaxSlugLength].Trim('-');
            }

            return string.IsNullOrWhiteSpace(slug) ? "clinic" : slug;
        }

        private static string BuildSlugWithSuffix(string baseSlug, int suffix)
        {
            var suffixText = $"-{suffix}";
            var maxBaseLength = MaxSlugLength - suffixText.Length;
            var trimmedBaseSlug = baseSlug.Length > maxBaseLength
                ? baseSlug[..maxBaseLength].Trim('-')
                : baseSlug;

            return $"{(string.IsNullOrWhiteSpace(trimmedBaseSlug) ? "clinic" : trimmedBaseSlug)}{suffixText}";
        }

        private async Task EnsureIdentityRoleAsync(ApplicationUser user, string role)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }

            if (!await _userManager.IsInRoleAsync(user, role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }
        }

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
