using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoodBite.Controllers;
using MoodBite.Data;
using MoodBite.Models;
using MoodBite.Services;
using MoodBite.Tests.TestSupport;
using MoodBite.ViewModels.Account;

namespace MoodBite.Tests.Integration;

public class AiAndEmailFallbackTests
{
    [Fact]
    public async Task Missing_gemini_key_meal_plan_generation_saves_standard_fallback()
    {
        await using var provider = TestIdentity.CreateProvider();
        var user = await TestIdentity.EnsureUserAsync(provider);
        var db = provider.GetRequiredService<ApplicationDbContext>();
        db.HealthProfiles.Add(Profile(user.Id));
        await db.SaveChangesAsync();

        var controller = new MealPlanController(
            db,
            provider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>(),
            provider.GetRequiredService<MealPlanService>(),
            TestIdentity.CreateGeminiService(provider),
            TestDb.Translation("en"));
        ApplyControllerContext(controller, user.Id);

        var result = await controller.GenerateAI();

        Assert.IsType<RedirectToActionResult>(result);
        var plan = Assert.Single(db.MealPlans);
        Assert.Equal("standard", plan.PlanType);
        Assert.Contains("days", plan.PlanJson);
    }

    [Fact]
    public async Task Missing_gemini_key_workout_generation_saves_fallback_plan()
    {
        await using var provider = TestIdentity.CreateProvider();
        var user = await TestIdentity.EnsureUserAsync(provider);
        var db = provider.GetRequiredService<ApplicationDbContext>();
        db.HealthProfiles.Add(Profile(user.Id));
        await db.SaveChangesAsync();

        var controller = new WorkoutController(
            db,
            provider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>(),
            TestIdentity.CreateGeminiService(provider),
            TestDb.Translation("en"));
        ApplyControllerContext(controller, user.Id);

        var result = await controller.Generate("beginner", "bodyweight", "30 min", "full body", 3);

        Assert.IsType<RedirectToActionResult>(result);
        var plan = Assert.Single(db.WorkoutPlans);
        Assert.Contains("Fallback workout plan", plan.PlanJson);
    }

    [Fact]
    public async Task Scanner_lookup_failure_returns_friendly_error_code_without_exception_detail()
    {
        await using var provider = TestIdentity.CreateProvider();
        var user = await TestIdentity.EnsureUserAsync(provider);
        var controller = new ScannerController(
            provider.GetRequiredService<ApplicationDbContext>(),
            provider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>(),
            new FakeHttpClientFactory(new HttpClient(new ThrowingHttpMessageHandler())),
            TestIdentity.CreateGeminiService(provider));
        ApplyControllerContext(controller, user.Id);

        var result = await controller.Lookup("123456789");

        var json = Assert.IsType<JsonResult>(result);
        Assert.Contains("service_unavailable", json.Value!.ToString());
        Assert.DoesNotContain("Exception", json.Value.ToString());
    }

    [Fact]
    public async Task Chatbot_missing_gemini_key_returns_friendly_reply_without_stack_trace()
    {
        await using var provider = TestIdentity.CreateProvider();
        var user = await TestIdentity.EnsureUserAsync(provider);
        var controller = new ChatController(
            TestIdentity.CreateGeminiService(provider),
            provider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>(),
            provider.GetRequiredService<ApplicationDbContext>(),
            TestDb.Translation("en"),
            provider.GetRequiredService<ILogger<ChatController>>());
        ApplyControllerContext(controller, user.Id);

        var result = await controller.Post(new ChatRequest { Message = "What should I eat today?" });

        var ok = Assert.IsType<OkObjectResult>(result);
        var text = ok.Value!.ToString();
        Assert.Contains("unavailable", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Stack Trace", text);
        Assert.DoesNotContain("InvalidOperationException", text);
    }

    [Fact]
    public async Task Password_reset_development_exposes_copy_link_safely()
    {
        await using var provider = TestIdentity.CreateProvider();
        var user = await TestIdentity.EnsureUserAsync(provider);
        var controller = CreateAccountController(provider, "Development");
        ApplyControllerContext(controller, user.Id);

        var result = await controller.ForgotPassword(new ForgotPasswordViewModel { Email = user.Email! });

        Assert.IsType<RedirectToActionResult>(result);
        Assert.True(controller.TempData.ContainsKey("ResetUrl"));
    }

    [Fact]
    public async Task Password_reset_production_does_not_expose_copy_link()
    {
        await using var provider = TestIdentity.CreateProvider();
        var user = await TestIdentity.EnsureUserAsync(provider);
        var controller = CreateAccountController(provider, "Production");
        ApplyControllerContext(controller, user.Id);

        var result = await controller.ForgotPassword(new ForgotPasswordViewModel { Email = user.Email! });

        Assert.IsType<RedirectToActionResult>(result);
        Assert.False(controller.TempData.ContainsKey("ResetUrl"));
    }

    private static AccountController CreateAccountController(ServiceProvider provider, string environmentName) =>
        new(
            provider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>(),
            provider.GetRequiredService<Microsoft.AspNetCore.Identity.SignInManager<ApplicationUser>>(),
            TestDb.Translation("en"),
            provider.GetRequiredService<ApplicationDbContext>(),
            new TestEnvironment { EnvironmentName = environmentName });

    private static void ApplyControllerContext(ControllerBase controller, string userId)
    {
        var httpContext = TestDb.HttpContextFor(userId);
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("example.test");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
            RouteData = new RouteData()
        };

        if (controller is Controller mvcController)
        {
            mvcController.TempData = new TempDataDictionary(httpContext, new InMemoryTempDataProvider());
            mvcController.Url = new TestUrlHelper();
        }
    }

    private static HealthProfile Profile(string userId) =>
        new()
        {
            UserId = userId,
            Age = 30,
            Gender = "female",
            Height = 168,
            Weight = 72,
            Goal = "loseWeight",
            ActivityLevel = "moderate",
            CookingStyle = "moderate",
            Budget = "medium",
            DietSlug = "mediterranean",
            CalorieTarget = 1700,
            WaterGoal = 8
        };
}

internal sealed class ThrowingHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        throw new HttpRequestException("network unavailable with sensitive detail");
}

internal sealed class InMemoryTempDataProvider : ITempDataProvider
{
    private readonly Dictionary<string, object> _data = new();

    public IDictionary<string, object> LoadTempData(HttpContext context) => _data;

    public void SaveTempData(HttpContext context, IDictionary<string, object> values)
    {
        _data.Clear();
        foreach (var pair in values)
        {
            _data[pair.Key] = pair.Value;
        }
    }
}

internal sealed class TestUrlHelper : IUrlHelper
{
    public ActionContext ActionContext { get; } =
        new(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());

    public string? Action(UrlActionContext actionContext)
    {
        var action = actionContext.Action ?? "Index";
        var controller = actionContext.Controller ?? "Home";
        var query = string.Empty;

        if (actionContext.Values != null)
        {
            var values = actionContext.Values
                .GetType()
                .GetProperties()
                .Select(property => new { property.Name, Value = property.GetValue(actionContext.Values)?.ToString() })
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
                .Select(pair => $"{Uri.EscapeDataString(pair.Name)}={Uri.EscapeDataString(pair.Value!)}");
            query = string.Join("&", values);
        }

        var url = $"https://example.test/{controller}/{action}";
        return string.IsNullOrWhiteSpace(query) ? url : $"{url}?{query}";
    }

    public string? Content(string? contentPath) => contentPath;

    public bool IsLocalUrl(string? url) => !string.IsNullOrWhiteSpace(url) && url.StartsWith('/');

    public string? Link(string? routeName, object? values) => RouteUrl(new UrlRouteContext { RouteName = routeName, Values = values });

    public string? RouteUrl(UrlRouteContext routeContext) => "https://example.test/";
}
