using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecommendationApp.Data;
using RecommendationApp.Models;
using RecommendationApp.Services;
using System.Text.Json;

namespace RecommendationApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        private readonly RecommendationService _recService;

        public HomeController(AppDbContext db, RecommendationService recService)
        {
            _db = db;
            _recService = recService;
        }

        private int? GetUserId() => HttpContext.Session.GetInt32("UserId");

        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var user = await _db.Users.FindAsync(userId.Value);
            var behaviors = await _db.Behaviors.Where(b => b.UserId == userId).ToListAsync();
            var ratings = await _db.Ratings.Where(r => r.UserId == userId).ToListAsync();

            var categoryStats = await _db.Behaviors
                .Where(b => b.UserId == userId && b.Clicked == true)
                .Join(_db.Products, b => b.ProductId, p => p.ProductId, (b, p) => p.Category)
                .GroupBy(c => c)
                .Select(g => new CategoryStat
                {
                    Category = g.Key ?? "Other",
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            var recentlyViewed = await _db.Behaviors
                .Where(b => b.UserId == userId && b.Viewed == true)
                .OrderByDescending(b => b.BehaviorId)
                .Take(6)
                .Join(_db.Products, b => b.ProductId, p => p.ProductId, (b, p) => p)
                .ToListAsync();

            var (recommendations, fitness) = await _recService.GetRecommendationsAsync(userId.Value);

            return View(new DashboardViewModel
            {
                User = user!,
                TotalViewed = behaviors.Count(b => b.Viewed == true),
                TotalClicked = behaviors.Count(b => b.Clicked == true),
                TotalPurchased = behaviors.Count(b => b.Purchased == true),
                AverageRating = ratings.Any() ? Math.Round(ratings.Average(r => r.RatingValue), 1) : 0,
                CategoryStats = categoryStats,
                RecentlyViewed = recentlyViewed,
                Recommendations = recommendations,
                FitnessScore = fitness
            });
        }

        public async Task<IActionResult> Products(string? category, decimal? minPrice, decimal? maxPrice, string? search)
        {
            if (GetUserId() == null) return RedirectToAction("Login", "Account");

            var query = _db.Products.AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => p.Category == category);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Category!.Contains(search));

            ViewBag.Categories = await _db.Products.Select(p => p.Category).Distinct().ToListAsync();
            ViewBag.SelectedCategory = category;
            ViewBag.Search = search;

            return View(await query.ToListAsync());
        }

        public async Task<IActionResult> ProductDetail(int id)
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            await _recService.TrackBehaviorAsync(userId.Value, id, "view");

            var avgRating = await _db.Ratings
                .Where(r => r.ProductId == id)
                .AverageAsync(r => (double?)r.RatingValue) ?? 0;

            var purchases = await _db.Behaviors
                .CountAsync(b => b.ProductId == id && b.Purchased == true);

            var views = await _db.Behaviors
                .CountAsync(b => b.ProductId == id && b.Viewed == true);

            var userRating = await _db.Ratings
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ProductId == id);

            var userBehavior = await _db.Behaviors
                .FirstOrDefaultAsync(b => b.UserId == userId && b.ProductId == id);

            return View(new ProductViewModel
            {
                Product = product,
                AverageRating = Math.Round(avgRating, 1),
                TotalPurchases = purchases,
                TotalViews = views,
                UserRating = userRating?.RatingValue ?? 0,
                UserPurchased = userBehavior?.Purchased == true,
                CategoryIcon = CategoryHelper.GetIcon(product.Category),
                CategoryColor = CategoryHelper.GetColor(product.Category)
            });
        }

        public IActionResult Cart()
        {
            if (GetUserId() == null) return RedirectToAction("Login", "Account");

            return View(GetCart());
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId)
        {
            var userId = GetUserId();

            if (userId == null)
                return Json(new { success = false });

            var product = await _db.Products.FindAsync(productId);

            if (product == null)
                return Json(new { success = false });

            await _recService.TrackBehaviorAsync(userId.Value, productId, "click");

            var cart = GetCart();
            var existingItem = cart.FirstOrDefault(c => c.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = productId,
                    ProductName = $"Product #{productId}",
                    Price = product.Price ?? 0,
                    Quantity = 1,
                    CategoryIcon = CategoryHelper.GetIcon(product.Category)
                });
            }

            SaveCart(cart);

            return Json(new
            {
                success = true,
                count = cart.Sum(c => c.Quantity)
            });
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = GetCart();

            cart.RemoveAll(c => c.ProductId == productId);

            SaveCart(cart);

            return RedirectToAction("Cart");
        }

        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            var userId = GetUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var cart = GetCart();

            foreach (var item in cart)
            {
                await _recService.TrackBehaviorAsync(userId.Value, item.ProductId, "purchase");
            }

            TempData["OrderTotal"] = cart.Sum(c => c.Price * c.Quantity).ToString("F2");
            TempData["OrderItems"] = cart.Sum(c => c.Quantity).ToString();

            SaveCart(new List<CartItem>());

            return RedirectToAction("OrderConfirmation");
        }

        public IActionResult OrderConfirmation()
        {
            if (GetUserId() == null)
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Rate(int productId, int rating)
        {
            var userId = GetUserId();

            if (userId == null)
                return Json(new { success = false });

            await _recService.SaveRatingAsync(userId.Value, productId, rating);

            return Json(new { success = true });
        }

        [HttpGet]
        public IActionResult GetCartCount()
        {
            return Json(new
            {
                count = GetCart().Sum(c => c.Quantity)
            });
        }

        [HttpGet]
        public async Task<IActionResult> ApiProducts()
        {
            return Json(await _db.Products
                .Select(p => new
                {
                    product_id = p.ProductId,
                    category = p.Category,
                    price = p.Price
                })
                .ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> ApiBehavior()
        {
            return Json(await _db.Behaviors
                .Select(b => new
                {
                    user_id = b.UserId,
                    product_id = b.ProductId,
                    viewed = b.Viewed,
                    clicked = b.Clicked,
                    purchased = b.Purchased
                })
                .ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> ApiRatings()
        {
            return Json(await _db.Ratings
                .Select(r => new
                {
                    user_id = r.UserId,
                    product_id = r.ProductId,
                    rating = r.RatingValue
                })
                .ToListAsync());
        }

        private List<CartItem> GetCart()
        {
            var json = HttpContext.Session.GetString("Cart");

            return string.IsNullOrEmpty(json)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));
        }
    }

}
}