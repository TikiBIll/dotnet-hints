ASP.NET External Cookie Authentication
==============================

Full code example on how to use an authorization cookie generated out of ASP.NET to authenticate a user.

Useful Links:
* Issue with more links, description: https://github.com/MohammadYounes/MVC5-MixedAuth/issues/20
* asp.net Cookie Middleware: https://docs.asp.net/en/latest/security/authentication/cookie.html

In Startup.cs:
```c#
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationScheme = ExternalCookieAuth.aspAuthCookieSuffix,
                //LoginPath = new PathString("/Account/Unauthorized/"), //Need "using Microsoft.AspNetCore.Http;" for PathString
                //AccessDeniedPath = new PathString("/Account/Forbidden/"),
                AutomaticAuthenticate = true, //Needs to be true for external auth-cookie.
                AutomaticChallenge = false, //this flag indicates that the middleware should redirect the browser to the LoginPath or AccessDeniedPath when authorization fails.
            });

            //After app.UseCookieAuthentication
            app.ExternalCookieAuth();
```

ExternalCookieAuth.cs:
```c#
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace Custom.Http.Auth
{
    public static class ExternalCookieAuthExtensions
    {
        public static IApplicationBuilder ExternalCookieAuth(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExternalCookieAuth>();
        }
    }

    public class ExternalCookieAuth
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        /// <summary>
        /// The name of the cookie set by another app/web-page.
        /// </summary>
        private readonly string extAuthCookieName = "InsecureAuth"; //Because in this demo its not encrypted or hashed.

        /// <summary>
        /// asp.net will name the cookie e.g. ".AspNetCore.ExternalAuth" if this value
        /// is set to ExternalAuth.
        /// 
        /// Since it is a suffix, it could be named the same as our external auth
        /// cookie, it is named differently for demonstration purposes.
        /// 
        /// Static field so that we can access it from Startup.cs as the 
        /// CookieAuthenticationOptions.AuthenticationScheme has to match.
        /// </summary>
        public static string aspAuthCookieSuffix = "ExternalAuth";

        public ExternalCookieAuth(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<ExternalCookieAuth>();
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Cookies != null)
            {
                var cookies = context.Request.Cookies.Where(cookie => cookie.Key == extAuthCookieName);
                foreach (var cookie in cookies)
                {
                    bool validCookie = false;

                    //Check if the cookie is valid. Your security goes here.
                    var values = cookie.Value.Split(':');
                    validCookie = values[0] != null && values[0].Equals("bill") ? true : false;

                    if (validCookie)
                    {
                        _logger.LogInformation("Have Valid Auth Cookie, Set Claims Principal");
                        //Everything checks out.
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, values?[0]), //Standard claim name.
                            new Claim("EmployeeId", values?[1]), //Custom claim name.
                            new Claim("Mood", values?[2]),
                        };

                        //Important point, by naming the ClaimsIdentity (second parameter), it sets isAuthenticated to true.
                        ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "SomeClaimName");
                        var user = new ClaimsPrincipal(claimsIdentity);
                        await context.Authentication.SignInAsync(aspAuthCookieSuffix, user);
                    }
                }
            }

            //HACK: for example code, just set us as logged in after the first call.
            CookieOptions opt = new CookieOptions();
            opt.Expires = DateTime.Now.AddDays(1);
            context.Response.Cookies.Append(extAuthCookieName, "bill:1234:happy", opt);

            await _next.Invoke(context);
            _logger.LogInformation("Custom Auth Finished handling request.");
        }
    }
}
```
