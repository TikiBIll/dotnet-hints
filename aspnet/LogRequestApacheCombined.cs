
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Linq;

namespace Http.Extensions
{
    public static class LogRequestApacheCombinedExtensions
    {
        /// <summary>
        /// Logs requests in the apache combined format. This should be added
        /// very early in the middleware pipeline. Uses LogInformation() log
        /// level.
        ///
        /// Gets the user name from the User Claim WindowsAccountName.
        /// </summary>
        public static IApplicationBuilder LogRequestApacheCombined(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LogRequestApacheCombined>();
        }
    }

    public class LogRequestApacheCombined
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public LogRequestApacheCombined(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<LogRequestApacheCombined>();
        }

        public async Task Invoke(HttpContext context)
        {
            string originalPath = WebUtility.UrlDecode( context.Request.Path );
            await _next.Invoke(context); //Handle everything first.

            _logger.LogInformation("{0} - {1} [{2}] \"{3} {4} {5}\" {6} {7} \"{8}\" \"{9}\"",
            context.Connection.RemoteIpAddress.ToString(), //Should use an extension that checks X-Forwarded-For header.
            context.User.HasClaim(claim => claim.Type == ClaimTypes.WindowsAccountName )?
                context.User.Claims.Where( c => c.Type == ClaimTypes.WindowsAccountName ).FirstOrDefault().Value : "-",
                System.DateTime.Now.ToString("dd/MMM/yyyy:HH:mm:ss K"),
                context.Request.Method,
                originalPath,
                context.Request.Protocol,
                context.Response.StatusCode,
                context.Response.ContentLength ?? 0, //Not known without inspecting the body bufffer.
                context.Request.Headers["Referer"].FirstOrDefault(), //Misspelled forever.
                context.Request.Headers["User-Agent"].FirstOrDefault()
            );
        }
    }
}
