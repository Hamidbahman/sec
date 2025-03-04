using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Authentication.Application;

public class CheckboxCaptchaService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CheckboxCaptchaService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GenerateCaptchaToken()
    {
        var token = Guid.NewGuid().ToString();

        var context = _httpContextAccessor.HttpContext;
        if (context != null && context.Session != null)
        {
            context.Session.SetString("CaptchaToken", token);
            context.Session.SetString("CaptchaTimestamp", DateTime.UtcNow.ToString());
        }

        return token;
    }

    public bool ValidateCaptchaToken(string userToken)
{
    var context = _httpContextAccessor.HttpContext;

    if (context != null && context.Session != null)
    {
        var storedToken = context.Session.GetString("CaptchaToken");
        var timestampStr = context.Session.GetString("CaptchaTimestamp");

        if (storedToken == null || timestampStr == null)
            return false;

        if (!DateTime.TryParse(timestampStr, out var timestamp))
            return false;

        if ((DateTime.UtcNow - timestamp).TotalMinutes > 5)
            return false;

        return storedToken == userToken;
    }

    return false;
}

}
