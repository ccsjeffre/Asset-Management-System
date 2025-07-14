using Asset_Management_System.Models;
using Microsoft.AspNetCore.Mvc;


namespace Asset_Management_System.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext context;

        public AccountController(ApplicationDbContext context)
        {
            this.context = context;
        }

        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register(User user)
        {
            string? confirmPassword = Request.Form["ConfirmPassword"];

            if (user.Password != confirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Password and confirm password do not match.");
                return View(user);
            }

            if (context.Users.Any(u => u.SchoolID == user.SchoolID))
            {
                ModelState.AddModelError("SchoolID", "This School ID is already registered.");
            }

            if (context.Users.Any(u => u.Username == user.Username))
            {
                ModelState.AddModelError("Username", "This username is already taken.");
            }

            if (context.Users.Any(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "This email is already in use.");
            }

            if (!ModelState.IsValid)
            {
                return View(user);
            }

            context.Users.Add(user);
            context.SaveChanges();

            return RedirectToAction("Login");
        }
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var userByUsername = context.Users.FirstOrDefault(u => u.Username == username);

            if (userByUsername == null)
            {
                ModelState.AddModelError("username", "invalid username");
                return View();
            }
            if (userByUsername.Password != password)
            {
                ModelState.AddModelError("password", "invalid password");
                return View();
            }

            HttpContext.Session.SetString("SchoolID", userByUsername.SchoolID ?? "N/A");
            HttpContext.Session.SetString("FullName", $"{userByUsername.FirstName} {userByUsername.LastName}");
            HttpContext.Session.SetString("Role", userByUsername.Role ?? "Unknown");


            return RedirectToAction("Dashboard", "LandingPage");
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
