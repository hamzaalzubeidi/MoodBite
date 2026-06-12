using System.Net;
using MoodBite.Constants;
using MoodBite.Tests.TestSupport;

namespace MoodBite.Tests.Integration;

public class RouteSmokeTests : IClassFixture<MoodBiteWebApplicationFactory>
{
    private readonly MoodBiteWebApplicationFactory _factory;

    public RouteSmokeTests(MoodBiteWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public static TheoryData<string, HttpStatusCode> PublicRoutes => new()
    {
        { "/", HttpStatusCode.OK },
        { "/Home/Privacy", HttpStatusCode.OK },
        { "/Account/Login", HttpStatusCode.OK },
        { "/Account/Register", HttpStatusCode.OK },
        { "/Account/ForgotPassword", HttpStatusCode.OK },
        { "/Diets", HttpStatusCode.OK },
        { "/Diets/Detail/mediterranean", HttpStatusCode.OK },
        { "/Error/404", HttpStatusCode.NotFound },
        { "/Account/AccessDenied", HttpStatusCode.Forbidden }
    };

    public static TheoryData<string> PatientRoutes => new()
    {
        "/Dashboard",
        "/Profile",
        "/MealPlan",
        "/MealPlan/History",
        "/MealPlan/ShoppingList",
        "/Scanner",
        "/Scanner/MyScanHistory",
        "/Report",
        "/Progress",
        "/Weight",
        "/Workout",
        "/Restaurants",
        "/Emergency",
        "/Notifications",
        "/Community",
        "/Challenge",
        "/Buddy",
        "/Achievements"
    };

    public static TheoryData<string> AdminRoutes => new()
    {
        "/Admin",
        "/Admin/AdminDashboard",
        "/Admin/AdminUsers",
        "/Admin/AdminClinics",
        "/Admin/AdminDiets",
        "/Admin/AdminRecipes"
    };

    public static TheoryData<string> ClinicRoutes => new()
    {
        "/Clinic",
        "/Clinic/ClinicDashboard",
        "/Clinic/ClinicSettings",
        "/Clinic/ClinicStaff",
        "/Clinic/Patients",
        "/Clinic/MealPlans",
        "/Clinic/Appointments"
    };

    [Theory]
    [MemberData(nameof(PublicRoutes))]
    public async Task Public_routes_render_expected_status_without_developer_exception_page(
        string path,
        HttpStatusCode expectedStatus)
    {
        var client = _factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.DoesNotContain("Developer Exception Page", body);
        Assert.DoesNotContain("Stack Trace", body);
    }

    [Theory]
    [MemberData(nameof(PatientRoutes))]
    public async Task Patient_routes_redirect_to_login_when_unauthenticated(string path)
    {
        var client = _factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Account/Login", RedirectPath(response));
    }

    [Theory]
    [MemberData(nameof(PatientRoutes))]
    public async Task Patient_routes_render_for_authenticated_patient_without_unhandled_exceptions(string path)
    {
        var client = await _factory.CreateAuthenticatedClientAsync("patient-route", ApplicationRoles.User);

        var response = await client.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("Developer Exception Page", body);
        Assert.DoesNotContain("Stack Trace", body);
    }

    [Theory]
    [MemberData(nameof(AdminRoutes))]
    public async Task Admin_routes_require_login_when_unauthenticated(string path)
    {
        var client = _factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Account/Login", RedirectPath(response));
    }

    [Theory]
    [MemberData(nameof(AdminRoutes))]
    public async Task Admin_routes_render_for_admin_without_unhandled_exceptions(string path)
    {
        var client = await _factory.CreateAuthenticatedClientAsync("admin-route", ApplicationRoles.Admin);

        var response = await client.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("Developer Exception Page", body);
        Assert.DoesNotContain("Stack Trace", body);
    }

    [Theory]
    [MemberData(nameof(ClinicRoutes))]
    public async Task Clinic_routes_require_login_when_unauthenticated(string path)
    {
        var client = _factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Account/Login", RedirectPath(response));
    }

    [Theory]
    [MemberData(nameof(ClinicRoutes))]
    public async Task Clinic_routes_render_for_clinic_owner_without_unhandled_exceptions(string path)
    {
        var client = await _factory.CreateAuthenticatedClientAsync("owner-route", ApplicationRoles.ClinicOwner);

        var response = await client.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("Developer Exception Page", body);
        Assert.DoesNotContain("Stack Trace", body);
    }

    [Theory]
    [InlineData("/Admin/AdminUsers")]
    [InlineData("/Admin/AdminClinics")]
    public async Task Non_admin_user_gets_forbidden_or_access_denied_for_admin_routes(string path)
    {
        var client = await _factory.CreateAuthenticatedClientAsync("patient-route", ApplicationRoles.User);

        var response = await client.GetAsync(path);

        Assert.True(response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Redirect);
    }

    private static string? RedirectPath(HttpResponseMessage response)
    {
        var location = response.Headers.Location;
        return location?.IsAbsoluteUri == true ? location.PathAndQuery : location?.OriginalString;
    }
}
