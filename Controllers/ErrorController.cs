using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MoodBite.Services;
using MoodBite.ViewModels;

namespace MoodBite.Controllers
{
    [AllowAnonymous]
    public class ErrorController : Controller
    {
        private readonly TranslationService _t;

        public ErrorController(TranslationService t)
        {
            _t = t;
        }

        [HttpGet("/Error/{statusCode:int?}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Status(int? statusCode)
        {
            var code = statusCode ?? 500;
            Response.StatusCode = code;

            if (WantsJsonResponse())
            {
                return StatusCode(code, new
                {
                    status = code,
                    message = SafeMessageKey(code)
                });
            }

            return View("Status", BuildModel(code));
        }

        private ErrorPageViewModel BuildModel(int code)
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            return code switch
            {
                401 => new ErrorPageViewModel
                {
                    StatusCode = 401,
                    Title = _t.Get("error.401.title"),
                    Message = _t.Get("error.401.message"),
                    Icon = "lock",
                    RequestId = requestId
                },
                403 => new ErrorPageViewModel
                {
                    StatusCode = 403,
                    Title = _t.Get("error.403.title"),
                    Message = _t.Get("error.403.message"),
                    Icon = "shield-alert",
                    RequestId = requestId
                },
                404 => new ErrorPageViewModel
                {
                    StatusCode = 404,
                    Title = _t.Get("error.404.title"),
                    Message = _t.Get("error.404.message"),
                    Icon = "search-x",
                    RequestId = requestId
                },
                _ => new ErrorPageViewModel
                {
                    StatusCode = 500,
                    Title = _t.Get("error.500.title"),
                    Message = _t.Get("error.500.message"),
                    Icon = "circle-alert",
                    RequestId = requestId
                }
            };
        }

        private bool WantsJsonResponse()
        {
            var statusFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
            var originalPath = statusFeature?.OriginalPath ?? Request.Path.Value ?? string.Empty;

            if (originalPath.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var requestedWith = Request.Headers.XRequestedWith.ToString();
            if (string.Equals(requestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return Request.Headers.Accept
                .Any(value => value?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
        }

        private string SafeMessageKey(int code) => code switch
        {
            401 => "unauthorized",
            403 => "forbidden",
            404 => "not_found",
            _ => "server_error"
        };
    }
}
