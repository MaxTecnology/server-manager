using Microsoft.AspNetCore.Mvc;
using SessionManager.Application.Common;

namespace SessionManager.WebApi.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected SessionManager.Application.Common.ActionContext BuildActionContext()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            username = "anonymous";
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        return new SessionManager.Application.Common.ActionContext(username, ip);
    }
}
