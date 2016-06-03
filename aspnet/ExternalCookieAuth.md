ASP.NET External Cookie Authentication
==============================

Full code example on how to use an authorization cookie generated outside of ASP.NET to authenticate a user.

Useful Links:
* Issue with more links, description: https://github.com/MohammadYounes/MVC5-MixedAuth/issues/20
* asp.net Cookie Middleware: https://docs.asp.net/en/latest/security/authentication/cookie.html

In Startup.cs:
```c#

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
        private readonly string extAuthCookieName = "InsecureAuth"; //Because in this demo it's not encrypted or hashed.


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
                        //See https://github.com/dotnet/corefx/blob/master/src/System.Security.Claims/src/System/Security/Claims/ClaimsIdentity.cs
                        ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "SomeClaimName");

                        //Now replace the (default) user claim with our new one.
                        context.User = new ClaimsPrincipal(claimsIdentity);                            
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
Then to access the claims in MVC/Razor pages:
```c#
@using System.Security.Claims;
...
@if( @User.Identity.IsAuthenticated ){
Hello @User.Claims?.Where( c => c.Type.Equals(ClaimTypes.WindowsAccountName)).FirstOrDefault()?.Value,
you seem @User.Claims?.Where( c => c.Type.Equals("Mood")).FirstOrDefault()?.Value. <br />
}

```
