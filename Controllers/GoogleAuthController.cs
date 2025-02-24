using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Pinpoint_Quiz.Controllers
{
    [Route("[controller]/[action]")]
    public class GoogleAuthController : Controller
    {
        [HttpGet]
        public IActionResult SignIn()
        {
            var authProps = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleCallback", "GoogleAuth")
            };
            // This challenges the user to sign in with Google
            return Challenge(authProps, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleCallback()
        {
            // This line finalizes the auth flow & obtains tokens
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }

            // This is how you get the user’s email
            var email = result.Principal.FindFirstValue(ClaimTypes.Email);

            // The access token for Classroom calls
            var accessToken = result.Properties.GetTokenValue("access_token");
            // The refresh token might also be needed for long-term usage
            var refreshToken = result.Properties.GetTokenValue("refresh_token");

            // Store in session or your Users table:
            // e.g., check if user with 'email' exists in your SQLite DB, if not, create new record

            // Then sign them into your own app’s session or identity
            // (You might already have a local session with userId for your DB)

            return RedirectToAction("Index", "Home");
        }
    }
}
