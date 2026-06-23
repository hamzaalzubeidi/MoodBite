namespace MoodBite.Services
{
    public static class ProductionConfigurationValidator
    {
        public static void Validate(WebApplication app)
        {
            var logger = app.Services.GetRequiredService<ILoggerFactory>()
                .CreateLogger("ProductionConfiguration");
            var configuration = app.Configuration;

            WarnIfMissing(logger, configuration.GetConnectionString("DefaultConnection"), "database connection string");
            WarnIfMissing(logger, configuration["OpenFoodFacts:BaseUrl"], "OpenFoodFacts base URL");
            WarnIfMissing(logger, configuration["OpenFoodFacts:UserAgent"], "OpenFoodFacts user agent");

            if (app.Environment.IsProduction())
            {
                WarnIfMissing(logger, configuration["AllowedHosts"], "AllowedHosts");
                if (string.Equals(configuration["AllowedHosts"], "*", StringComparison.Ordinal))
                {
                    logger.LogWarning("Production AllowedHosts is wildcard. Configure explicit host names before public deployment.");
                }

                if (!IsEmailConfigured(configuration))
                {
                    logger.LogWarning("Production email provider is not configured. Password reset and clinic invitation emails will not be delivered.");
                }

                if (string.IsNullOrWhiteSpace(configuration["Gemini:ApiKey"]))
                {
                    logger.LogWarning("Gemini API key is not configured. AI features will use fallbacks or friendly unavailable messages.");
                }
            }
            else
            {
                if (!IsEmailConfigured(configuration))
                {
                    logger.LogInformation("Email provider is not configured. Development flows will use copy-link previews where available.");
                }
            }
        }

        private static void WarnIfMissing(ILogger logger, string? value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                logger.LogWarning("Missing configuration: {ConfigurationName}.", name);
            }
        }

        private static bool IsEmailConfigured(IConfiguration configuration) =>
            string.Equals(configuration["Email:Provider"], "Smtp", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(configuration["Email:Smtp:Host"]) &&
            !string.IsNullOrWhiteSpace(configuration["Email:FromEmail"]);
    }
}
