using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecommendationApp.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("age")]
        public int? Age { get; set; }

        [Column("country")]
        public string? Country { get; set; }

        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
        public ICollection<Behavior> Behaviors { get; set; } = new List<Behavior>();
    }

    [Table("Products")]
    public class Product
    {
        [Key]
        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("category")]
        public string? Category { get; set; }

        [Column("price")]
        public decimal? Price { get; set; }

        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
        public ICollection<Behavior> Behaviors { get; set; } = new List<Behavior>();
    }

    [Table("Ratings")]
    public class Rating
    {
        [Key]
        [Column("rating_id")]
        public int RatingId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("rating")]
        public int RatingValue { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }
    }

    [Table("Behavior")]
    public class Behavior
    {
        [Key]
        [Column("behavior_id")]
        public int BehaviorId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("viewed")]
        public bool? Viewed { get; set; }

        [Column("clicked")]
        public bool? Clicked { get; set; }

        [Column("purchased")]
        public bool? Purchased { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Please enter UserID")]
        public int UserId { get; set; }
    }

    public class ProductViewModel
    {
        public Product Product { get; set; } = null!;
        public double AverageRating { get; set; }
        public int TotalPurchases { get; set; }
        public int TotalViews { get; set; }
        public int UserRating { get; set; }
        public bool UserPurchased { get; set; }
        public string CategoryIcon { get; set; } = "🛍️";
        public string CategoryColor { get; set; } = "#00d4ff";
    }

    public class DashboardViewModel
    {
        public User User { get; set; } = null!;
        public int TotalViewed { get; set; }
        public int TotalClicked { get; set; }
        public int TotalPurchased { get; set; }
        public double AverageRating { get; set; }
        public List<CategoryStat> CategoryStats { get; set; } = new();
        public List<Product> RecentlyViewed { get; set; } = new();
        public List<ProductViewModel> Recommendations { get; set; } = new();
        public double FitnessScore { get; set; }
    }

    public class CategoryStat
    {
        public string Category { get; set; } = "";
        public int Count { get; set; }
    }

    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string CategoryIcon { get; set; } = "🛍️";
    }

    public class GaRecommendationResponse
    {
        public List<int> Recommendations { get; set; } = new();
        public double FitnessScore { get; set; }
        public string Message { get; set; } = "";
    }
}
