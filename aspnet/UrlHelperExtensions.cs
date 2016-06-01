using System.Text;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;


namespace Http.Extensions
{
    /// <summary>
    /// Some helper extensions for e.g. easily bookmarkable URIs and whatnot. Especially useful in a
    /// corporate environment where one needs a user to provide a link to the web page giving him/her the
    /// hassle.
    /// </summary>
    /// <remarks>
    /// "Inspiration" from original UriHelper class. Ignore the code similarities.
    /// See https://github.com/aspnet/HttpAbstractions/blob/dev/src/Microsoft.AspNetCore.Http.Extensions/UriHelper.cs
    /// </remarks>
    public static class UriHelperExtensions
    {
        // Define this ourselves. The original extensions has it, but it is private.
        private const string SchemeDelimiter = "://";

        /// <summary>
        ///Hopefully the web site works with JavaScript disabled. So then how to
        ///  prevent double-submits on new record creation? If one has a hidden
        ///  field that is a GUID, one can check for the existence of that GUID
        ///  in the database before creating a new record. While not as elegant as
        ///  disabling the back button onClick, it works without JavaScript, and
        ///  as a backup preventive measure.
        ///
        ///It matters here because if the user was creating a new record and copies
        ///  the URL, the form-guid will not be present and another person clicking on the
        ///  same link will not accidently create/update the same record again because
        ///  hopefully the lack of a form-guid will raise an error. 
        /// </summary>
        //
        // TODO: Make a list of fields?
        private const string ignoreField = "form-guid";

        //Does the compiler optimize this out if I do the concatenation in the check?
        private const string ignoreFieldEquals = ignoreField + "=";

        /// <summary>
        /// Returns the combined components of the request URL in a fully un-escaped
        /// form (including the QueryString) with all blank &amp;value= parameters
        /// removed. Also handles post data, converting the POST into a GET URI.
        /// This allows adding a "Bookmarkable Link" link on a page that retains form
        /// values.
        /// 
        /// Use with caution for forms that perform record creation or updates.
        /// 
        /// Browsers have a limit to the length of a GET URI so the conversion of
        /// a POST into a GET could loose information by being truncated.
        /// 
        /// </summary>
        /// <param name="request">HttpRequest</param>
        /// <param name="newHost">Replacement host for use in a reverse proxy situation.</param>
        /// <returns>Un-escaped absolute URL</returns>
        /// <example>
        /// Example .cshtml bookmarkable link
        /// <code>
        /// @using static Http.Extensions.UriHelperExtensions;
        /// ...
        /// &lt;a href="@Html.Raw( @Context.Request.GetBookmarkUrl())"&gt;Bookmarkable Link for this Page&lt;/a&gt;
        /// </code>
        /// </example>
        //
        //Non encoded example .cshtml: <a href="@Html.Raw( @Context.Request.GetBookmarkUrl())">Bookarkable Link</a>
        public static string GetBookmarkUrl(this HttpRequest request, string newHost = null)
        {
            var host = newHost ?? request.Host.Value;
            var pathBase = request.PathBase.Value;
            var path = request.Path.Value;
            string queryString;

            if (request.Method.Equals("GET"))
            {
                //Split the values on the ampersand, remove anything that ends in an equals,
                // and re-join on the ampersand.
                queryString = string.Join("&", request.QueryString.Value
                    .Split('&')
                    .Where(param => param.Length > 0 && !param.Substring(param.Length - 1).Equals("=")
                    && param.IndexOf(ignoreFieldEquals) != 0 ? true : false));
            }
            else
            {
                //Should probably only support POST.
                var tempQueryString = new StringBuilder();
                bool first = true;
                foreach (var key in request.Form.Keys)
                {
                    if (key == null || key.Length == 0)
                    {
                        //Safety check. Hopefully we will never be passed such things.
                        continue;
                    }
                    else if (key.Equals(ignoreField))
                    {
                        continue;
                    }

                    foreach (var value in request.Form[key])
                    {
                        //Multiple values are possible, with a GET the URL would have e.g. ?x=coffee&x=sugar
                        if (value != null && value.Length > 0)
                        {
                            tempQueryString.Append(first ? "?" : "&");
                            first = false;
                            tempQueryString.Append(UrlEncoder.Default.Encode(key));
                            tempQueryString.Append("=");
                            tempQueryString.Append(UrlEncoder.Default.Encode(value));
                        }
                    }
                }
                queryString = tempQueryString.ToString();
            }

            // PERF: Calculate string length to allocate correct buffer size for StringBuilder.
            var length = request.Scheme.Length + SchemeDelimiter.Length + host.Length
                + pathBase.Length + path.Length + queryString.Length;

            return new StringBuilder(length)
                .Append(request.Scheme)
                .Append(SchemeDelimiter)
                .Append(host)
                .Append(pathBase)
                .Append(path)
                .Append(queryString)
                .ToString();
        }
    }
}