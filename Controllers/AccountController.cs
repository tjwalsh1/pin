using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Pinpoint_Quiz.Services;
using Pinpoint_Quiz.Dtos;
using Pinpoint_Quiz.Models;

namespace Pinpoint_Quiz.Controllers
{
    [Route("[controller]")]
    public class AccountController : Controller
    {
        private readonly AccountService _accountService;
        private readonly ILogger<AccountController> _logger;
        private readonly AccoladeService _accoladeService;

        public AccountController(AccountService accountService, ILogger<AccountController> logger, AccoladeService accoladeService)
        {
            _accountService = accountService;
            _logger = logger;
            _accoladeService = accoladeService;
        }

        // GET: /Account/Register
        [HttpGet("register")]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost("register")]
        public IActionResult Register(RegisterDto dto)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            bool registered = _accountService.RegisterUser(dto);
            if (!registered)
            {
                ModelState.AddModelError("", "Registration failed. Email might be in use.");
                return View(dto);
            }

            // Auto-login
            int? userId = _accountService.LoginUser(dto.Email, dto.Password);
            if (userId.HasValue)
            {
                HttpContext.Session.SetInt32("UserId", userId.Value);

                // Optionally store initials in session
                var initials = $"{dto.FirstName?[0]}{dto.LastName?[0]}".ToUpper();
                HttpContext.Session.SetString("UserInitials", initials);

                return RedirectToAction("Profile");
            }
            return RedirectToAction("Login");
        }

        // GET: /Account/Login
        [HttpGet("login")]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost("login")]
        public IActionResult Login(LoginDto dto)
        {
            var userId = _accountService.LoginUser(dto.Email, dto.Password);
            if (!userId.HasValue)
            {
                ModelState.AddModelError("", "Invalid credentials.");
                ViewBag.ErrorMessage = "Invalid email or password.";
                return View(dto);
            }
            HttpContext.Session.SetInt32("UserId", userId.Value);

            // Optional: store user initials
            var user = _accountService.GetUserById(userId.Value);
            if (user != null)
            {
                var initials = $"{user.FirstName[0]}{user.LastName[0]}".ToUpper();
                HttpContext.Session.SetString("UserInitials", initials);
                HttpContext.Session.SetString("UserRole", user.UserRole);
            }

            return RedirectToAction("Profile");
        }

        // GET: /Account/Profile
        [HttpGet("profile")]
        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            var user = _accountService.GetUserById(userId.Value);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var accolades = _accoladeService.GetAccoladesForUser(userId.Value);

            // If you want to show recent quizzes or accolades, fetch them here:
            var profileVm = new ProfileViewModel
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Grade = user.Grade ?? 0,
                ClassId = user.ClassId ?? 0,
                SchoolId = user.SchoolId ?? 0,
                ProficiencyMath = user.ProficiencyMath,
                ProficiencyEbrw = user.ProficiencyEbrw,
                OverallProficiency = user.OverallProficiency,
                // Additional properties if needed
                Dates = new System.Collections.Generic.List<string>(),
                MathProficiencies = new System.Collections.Generic.List<double>(),
                EbrwProficiencies = new System.Collections.Generic.List<double>(),
                Accolades = accolades
            };

            // Example: fetch user accolades from your Accolade table
            // profileVm.Accolades = accoladeService.GetAccoladesForUser(user.Id);

            // Example: fetch last 5 quizzes
            // profileVm.RecentQuizzes = quizService.GetQuizHistory(user.Id)
            //                                     .Take(5)
            //                                     .ToList();

            return View(profileVm);
        }

        // POST: /Account/Logout
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Login");
        }
    }
}
