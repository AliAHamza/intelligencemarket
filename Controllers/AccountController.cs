using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecommendationApp.Data;
using RecommendationApp.Models;

namespace RecommendationApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        public AccountController(AppDbContext db) { _db = db; }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _db.Users.FindAsync(model.UserId);
            if (user == null)
            {
                ModelState.AddModelError("", "UserID not found, Please enter another UserID from 1 to 1000.");
                return View(model);
            }

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserCountry", user.Country ?? "Not Specified");
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }

}
}
