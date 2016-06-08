
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Http.Extensions
{
    public static class DevUrlRewriteExtensions
    {
        /// <summary>
        /// Looks for /DEV/ and/or /U=xyz/ in the url path and removes them
        /// and also sets DEV (true) and DEV_USER respectivly in the context.Items
        /// dictionary.  This is useful for e.g. connecting to a test database
        /// so the production code is used but against said test DB. Useful
        /// for for training and testing.
        /// 
        /// This should be place in the pipeline before static files and after
        /// any URL logging since it alters the URL the rest of the pipeline sees.
        /// </summary>
        public static IApplicationBuilder DevUrlRewrite(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DevUrlRewrite>();
        }
    }

    public class DevUrlRewrite
    {
        private readonly RequestDelegate _next;

        private readonly ILogger _logger;

        //The U=user in the path is encoded, so use %3D for the equals sign.
        private readonly Regex _devUserRgx = new Regex("(.*)/U%3D([a-zA-Z]+)/(.*)", RegexOptions.IgnoreCase);

        public DevUrlRewrite(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<DevUrlRewrite>();
        }

        public async Task Invoke(HttpContext context)
        {
            bool pathStringAltered = false;
            string pathString = context.Request.Path.ToString();
            if (pathString.Contains("/DEV/"))
            {
                pathStringAltered = true;
                pathString = pathString.Replace("/DEV/", "/");
                _logger.LogTrace($"Have /DEV/, new path is {pathString}", pathString);
                context.Items.Add("DEV", true); //So the downstream app knows which database to use.
            }

            Match match = _devUserRgx.Match(pathString);
            if (match.Success)
            {
                pathStringAltered = true;
                context.Items.Add("DEV_USER", match.Groups[2].Value);
                pathString = (match.Groups[1].Value ?? "") + "/" + (match.Groups[3].Value ?? "");
                _logger.LogTrace("Found a DEV_USER = {0}, Path now {1}", match.Groups[1].Value, pathString);
            }

            if (pathStringAltered)
            {
                context.Request.Path = new PathString(pathString);
            }
            await _next.Invoke(context);
        }
    }
}
