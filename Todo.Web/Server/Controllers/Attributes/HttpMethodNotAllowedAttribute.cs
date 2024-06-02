using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Routing;

namespace Todo.Web.Server.Controllers.Attributes
{
    public class HttpMethodNotAllowedAttribute(params string[] httpMethods) : ActionMethodSelectorAttribute
    {
        private readonly string[] _httpMethods = httpMethods;

        public override bool IsValidForRequest(RouteContext routeContext, ActionDescriptor action)
        {
            var requestMethod = routeContext.HttpContext.Request.Method;

            return !_httpMethods.Contains(requestMethod);
        }
    }
}
