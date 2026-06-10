using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MoodBite.Constants;
using MoodBite.Data;

namespace MoodBite.Services
{
    public class CurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _db;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext db)
        {
            _httpContextAccessor = httpContextAccessor;
            _db = db;
        }

        public ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated =>
            Principal?.Identity?.IsAuthenticated == true;

        public string? GetCurrentUserId()
        {
            if (!IsAuthenticated)
            {
                return null;
            }

            var userId = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrWhiteSpace(userId) ? null : userId;
        }

        public Task<bool> IsCurrentUserPlatformAdminAsync(CancellationToken cancellationToken = default) =>
            IsUserPlatformAdminAsync(GetCurrentUserId(), cancellationToken);

        public Task<bool> IsUserPlatformAdminAsync(string? userId, CancellationToken cancellationToken = default) =>
            IsUserInRoleAsync(userId, ApplicationRoles.Admin, cancellationToken);

        public async Task<bool> IsUserInRoleAsync(
            string? userId,
            string role,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(role))
            {
                return false;
            }

            return await (
                from userRole in _db.UserRoles.AsNoTracking()
                join identityRole in _db.Roles.AsNoTracking()
                    on userRole.RoleId equals identityRole.Id
                where userRole.UserId == userId && identityRole.Name == role
                select userRole.UserId)
                .AnyAsync(cancellationToken);
        }
    }
}
