using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MoodBite.Constants;
using MoodBite.Data;
using MoodBite.Models;
using MoodBite.Services;
using MoodBite.ViewModels.Account;

namespace MoodBite.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly TranslationService _t;
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _environment;
        private readonly IEmailService _emailService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            TranslationService t,
            ApplicationDbContext db,
            IWebHostEnvironment environment,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _t = t;
            _db = db;
            _environment = environment;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToRoleHome(User);

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, _t.Get("auth.invalidCredentials", "ar") + " / Invalid email or password.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "حسابك معطّل. تواصل مع الدعم. / Your account is deactivated.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                // Set language preference from user
                HttpContext.Session.SetString("lang", user.PreferredLanguage);
                Response.Cookies.Append("lang", user.PreferredLanguage, new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    return Redirect(model.ReturnUrl);
                return await RedirectToRoleHomeAsync(user);
            }

            ModelState.AddModelError(string.Empty, "البريد الإلكتروني أو كلمة المرور غير صحيحة / Invalid email or password.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToRoleHome(User);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                IsActive = true,
                EmailConfirmed = true, // Skip email confirmation for now
                PreferredLanguage = "ar"
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, ApplicationRoles.User);
                await _signInManager.SignInAsync(user, isPersistent: false);

                // Set language
                HttpContext.Session.SetString("lang", "ar");
                Response.Cookies.Append("lang", "ar", new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

                return RedirectToAction("Index", "Profile");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.Action("ResetPassword", "Account",
                    new { token, email = model.Email }, Request.Scheme);

                if (!string.IsNullOrWhiteSpace(callbackUrl))
                {
                    var emailResult = await _emailService.SendPasswordResetAsync(model.Email, callbackUrl);
                    if (!string.IsNullOrWhiteSpace(emailResult.DevelopmentPreviewUrl))
                    {
                        TempData["ResetUrl"] = emailResult.DevelopmentPreviewUrl;
                    }
                }
            }

            TempData["Message"] = _environment.IsDevelopment()
                ? "إذا كان البريد الإلكتروني موجوداً، سيظهر رابط الإعادة هنا في بيئة التطوير فقط. / If the email exists, the reset link appears here in Development only."
                : "إذا كان البريد الإلكتروني موجوداً، سيتم إرسال رابط الإعادة عند تفعيل مزود البريد. / If the email exists, a reset link will be sent when email delivery is configured.";
            return RedirectToAction("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation() => View();

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            return View(new ResetPasswordViewModel { Token = token, Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["Success"] = true;
                return RedirectToAction("ResetPasswordConfirmation");
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                TempData["Success"] = true;
                return RedirectToAction("ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetLanguage(string lang, string? returnUrl = null)
        {
            var validLang = lang == "en" ? "en" : "ar";
            HttpContext.Session.SetString("lang", validLang);
            Response.Cookies.Append("lang", validLang, new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    user.PreferredLanguage = validLang;
                    await _userManager.UpdateAsync(user);
                }
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return View();
        }

        // ── My Account page ───────────────────────────────────────────────────

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MyAccount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var profile    = await _db.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            var totalLogs  = await _db.DayLogs.CountAsync(l => l.UserId == user.Id);
            var weightLogs = await _db.WeightLogs.CountAsync(w => w.UserId == user.Id);

            ViewBag.User        = user;
            ViewBag.Profile     = profile;
            ViewBag.TotalLogs   = totalLogs;
            ViewBag.WeightLogs  = weightLogs;
            ViewBag.MemberSince = user.CreatedAt.ToString("MMM yyyy");
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateName(string fullName)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (!string.IsNullOrWhiteSpace(fullName))
            {
                user.FullName = fullName.Trim();
                await _userManager.UpdateAsync(user);
                TempData["Success"] = _t.Get("account.nameUpdated");
            }
            return RedirectToAction("MyAccount");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
                TempData["Success"] = _t.Get("account.passwordChanged");
            else
                TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));

            return RedirectToAction("MyAccount");
        }
        private IActionResult RedirectToRoleHome(System.Security.Claims.ClaimsPrincipal principal)
        {
            if (principal.IsInRole(ApplicationRoles.Admin))
            {
                return RedirectToAction("Index", "AdminDashboard", new { area = "Admin" });
            }

            if (principal.IsInRole(ApplicationRoles.ClinicOwner) ||
                principal.IsInRole(ApplicationRoles.Dietitian) ||
                principal.IsInRole(ApplicationRoles.ClinicStaff))
            {
                return LocalRedirect("/Clinic");
            }

            return RedirectToAction("Index", "Dashboard", new { area = "" });
        }

        private async Task<IActionResult> RedirectToRoleHomeAsync(ApplicationUser user)
        {
            if (await _userManager.IsInRoleAsync(user, ApplicationRoles.Admin))
            {
                return RedirectToAction("Index", "AdminDashboard", new { area = "Admin" });
            }

            if (await _userManager.IsInRoleAsync(user, ApplicationRoles.ClinicOwner) ||
                await _userManager.IsInRoleAsync(user, ApplicationRoles.Dietitian) ||
                await _userManager.IsInRoleAsync(user, ApplicationRoles.ClinicStaff))
            {
                return LocalRedirect("/Clinic");
            }

            return RedirectToAction("Index", "Dashboard", new { area = "" });
        }
    }
}
