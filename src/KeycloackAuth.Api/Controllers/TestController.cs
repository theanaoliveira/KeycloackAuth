using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Client.AspNetCore.OpenIddictClientAspNetCoreConstants;

namespace KeycloackAuth.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpGet]
        [Authorize(AuthenticationSchemes = $"{CookieAuthenticationDefaults.AuthenticationScheme},{OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme}")]
        public IActionResult Test()
        {
        var dict = new Dictionary<string, string>();
        foreach (var claim in User.Claims)
        {
            dict.TryAdd(claim.Type, claim.Value);
        }
        
        return Ok(dict);
        }
    }
}
