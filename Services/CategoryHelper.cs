namespace RecommendationApp.Services
{
    public static class CategoryHelper
    {
        public static string GetIcon(string? category)
        {
            if (string.IsNullOrEmpty(category)) return "🛍";
            var c = category.ToLower();

            if (c.Contains("electron")  c.Contains("tech")  c.Contains("computer")  c.Contains("phone")  c.Contains("laptop"))
        return "💻";
            if (c.Contains("cloth")  c.Contains("fashion")  c.Contains("wear")  c.Contains("shirt")  c.Contains("dress"))
        return "👗";
            if (c.Contains("book") || c.Contains("read"))
                return "📚";
            if (c.Contains("food")  c.Contains("eat")  c.Contains("grocery") || c.Contains("drink"))
        return "🍕";
            if (c.Contains("sport")  c.Contains("fitness")  c.Contains("gym"))
        return "⚽️";
            if (c.Contains("home")  c.Contains("house")  c.Contains("furniture") || c.Contains("kitchen"))
        return "🏠";
            if (c.Contains("toy")  c.Contains("game")  c.Contains("kids") || c.Contains("child"))
        return "🎮";
            if (c.Contains("beauty")  c.Contains("cosmetic")  c.Contains("skin") || c.Contains("hair"))
        return "💄";
            if (c.Contains("health")  c.Contains("medical")  c.Contains("pharmacy"))
        return "💊";
            if (c.Contains("car")  c.Contains("auto")  c.Contains("vehicle"))
        return "🚗";
            if (c.Contains("travel")  c.Contains("bag")  c.Contains("luggage"))
        return "✈️";
            if (c.Contains("music")  c.Contains("audio")  c.Contains("headphone"))
        return "🎵";
            if (c.Contains("garden")  c.Contains("plant")  c.Contains("outdoor"))
        return "🌱";
            if (c.Contains("pet") || c.Contains("animal"))
                return "🐾";
            if (c.Contains("jewelry")  c.Contains("watch")  c.Contains("accessory"))
        return "💎";

            return "🛍";
        }

        public static string GetColor(string? category)
        {
            if (string.IsNullOrEmpty(category)) return "#00d4ff";
            var c = category.ToLower();

            if (c.Contains("electron") || c.Contains("tech")) return "#00d4ff";
            if (c.Contains("cloth") || c.Contains("fashion")) return "#f472b6";
            if (c.Contains("book")) return "#f59e0b";
            if (c.Contains("food")) return "#ef4444";
            if (c.Contains("sport")) return "#10b981";
            if (c.Contains("home")) return "#8b5cf6";
            if (c.Contains("toy") || c.Contains("game")) return "#f97316";
            if (c.Contains("beauty")) return "#ec4899";
            if (c.Contains("health")) return "#06b6d4";
            if (c.Contains("car")) return "#64748b";

            return "#7c3aed";
        }

        public static string GetGradient(string? category)
        {
            var color = GetColor(category);
            return $"linear-gradient(135deg, {color}22, {color}44)";
        }
    }
}
