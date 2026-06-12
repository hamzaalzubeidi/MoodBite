using MoodBite.Tests.TestSupport;

namespace MoodBite.Tests.Services;

public class TranslationServiceTests
{
    [Fact]
    public void Common_keys_resolve_in_english()
    {
        var service = TestDb.Translation("en");

        Assert.Equal("Dashboard", service.Get("nav.dashboard"));
        Assert.False(service.IsRtl);
    }

    [Fact]
    public void Common_keys_resolve_in_arabic()
    {
        var service = TestDb.Translation("ar");

        Assert.Equal("لوحة التحكم", service.Get("nav.dashboard"));
        Assert.True(service.IsRtl);
    }

    [Fact]
    public void Missing_keys_return_safe_key_fallback()
    {
        var service = TestDb.Translation("en");

        Assert.Equal("missing.key", service.Get("missing.key"));
    }

    [Fact]
    public void Unsupported_language_falls_back_to_english_value()
    {
        var service = TestDb.Translation("fr");

        Assert.Equal("Dashboard", service.Get("nav.dashboard"));
    }
}
