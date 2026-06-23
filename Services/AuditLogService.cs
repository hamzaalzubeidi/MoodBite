using System.Security.Claims;
using System.Text.Json;
using MoodBite.Data;
using MoodBite.Models;

namespace MoodBite.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(
            string action,
            string targetEntityType,
            string? targetEntityId = null,
            int? clinicId = null,
            string? targetUserId = null,
            string? summary = null,
            object? metadata = null,
            CancellationToken cancellationToken = default);
    }

    public class AuditLogService : IAuditLogService
    {
        private static readonly string[] SensitiveKeyParts =
        [
            "password",
            "token",
            "secret",
            "apikey",
            "api_key",
            "reset",
            "link",
            "url",
            "prompt",
            "content",
            "note",
            "medical"
        ];

        private static readonly string[] SensitiveValueParts =
        [
            "password=",
            "token=",
            "apikey=",
            "api_key=",
            "bearer ",
            "reset",
            "http://",
            "https://"
        ];

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(
            ApplicationDbContext db,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditLogService> logger)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task LogAsync(
            string action,
            string targetEntityType,
            string? targetEntityId = null,
            int? clinicId = null,
            string? targetUserId = null,
            string? summary = null,
            object? metadata = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(targetEntityType))
            {
                return;
            }

            try
            {
                var context = _httpContextAccessor.HttpContext;
                var user = context?.User;
                var roles = user?.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(v => v)
                    .ToArray() ?? [];

                _db.AuditLogs.Add(new AuditLog
                {
                    ActorUserId = Truncate(user?.FindFirstValue(ClaimTypes.NameIdentifier), 450),
                    ActorEmail = Truncate(user?.FindFirstValue(ClaimTypes.Email) ?? user?.Identity?.Name, 256),
                    ActorRoles = Truncate(string.Join(',', roles), 256),
                    ClinicId = clinicId,
                    TargetUserId = Truncate(targetUserId, 450),
                    TargetEntityType = TruncateRequired(targetEntityType, 80),
                    TargetEntityId = Truncate(targetEntityId, 120),
                    Action = TruncateRequired(action, 120),
                    Summary = TruncateRequired(summary ?? action, 500),
                    IpAddress = Truncate(context?.Connection.RemoteIpAddress?.ToString(), 64),
                    UserAgent = Truncate(context?.Request.Headers.UserAgent.ToString(), 500),
                    CreatedAtUtc = DateTime.UtcNow,
                    MetadataJson = SerializeSafeMetadata(metadata)
                });

                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Audit log write failed for action {Action} on {TargetEntityType}.", action, targetEntityType);
            }
        }

        private static string? SerializeSafeMetadata(object? metadata)
        {
            if (metadata == null)
            {
                return null;
            }

            var dictionary = metadata as IReadOnlyDictionary<string, object?> ?? ToDictionary(metadata);
            if (dictionary.Count == 0)
            {
                return null;
            }

            var safe = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in dictionary)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || IsSensitiveKey(pair.Key))
                {
                    continue;
                }

                var normalizedValue = NormalizeValue(pair.Value);
                if (ContainsSensitiveValue(normalizedValue))
                {
                    safe[TruncateRequired(pair.Key, 80)] = "[redacted]";
                    continue;
                }

                safe[TruncateRequired(pair.Key, 80)] = normalizedValue;
            }

            if (safe.Count == 0)
            {
                return null;
            }

            var json = JsonSerializer.Serialize(safe, JsonOptions);
            return Truncate(json, 2000);
        }

        private static Dictionary<string, object?> ToDictionary(object metadata)
        {
            return metadata.GetType()
                .GetProperties()
                .Where(property => property.GetIndexParameters().Length == 0)
                .ToDictionary(
                    property => property.Name,
                    property => property.GetValue(metadata),
                    StringComparer.OrdinalIgnoreCase);
        }

        private static object? NormalizeValue(object? value)
        {
            return value switch
            {
                null => null,
                string text => Truncate(text, 160),
                DateTime dateTime => dateTime,
                DateTimeOffset dateTimeOffset => dateTimeOffset,
                bool boolean => boolean,
                int integer => integer,
                long longValue => longValue,
                double doubleValue => doubleValue,
                decimal decimalValue => decimalValue,
                Enum enumValue => enumValue.ToString(),
                _ => Truncate(value.ToString(), 160)
            };
        }

        private static bool IsSensitiveKey(string key)
        {
            var normalized = key.Replace("-", string.Empty, StringComparison.Ordinal)
                .Replace("_", string.Empty, StringComparison.Ordinal)
                .ToLowerInvariant();

            return SensitiveKeyParts.Any(part => normalized.Contains(part.Replace("_", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal));
        }

        private static bool ContainsSensitiveValue(object? value)
        {
            if (value is not string text || string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var normalized = text.ToLowerInvariant();
            return SensitiveValueParts.Any(part => normalized.Contains(part, StringComparison.Ordinal));
        }

        private static string TruncateRequired(string value, int maxLength) =>
            Truncate(value, maxLength) ?? string.Empty;

        private static string? Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
        }
    }
}

