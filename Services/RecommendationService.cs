using Microsoft.EntityFrameworkCore;
using RecommendationApp.Data;
using RecommendationApp.Models;

namespace RecommendationApp.Services
{
    public class RecommendationService
    {
        private readonly AppDbContext _db;
        private readonly Random _random = new();

        private const int PopulationSize = 50;
        private const int Generations = 100;
        private const int RecommendationSize = 10;
        private const double MutationRate = 0.2;

        public RecommendationService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<(List<ProductViewModel> Products, double FitnessScore)> GetRecommendationsAsync(int userId)
        {
            var products = await _db.Products.ToListAsync();

            if (!products.Any())
                return (new List<ProductViewModel>(), 0);

            var behaviors = await _db.Behaviors.ToListAsync();
            var ratings = await _db.Ratings.ToListAsync();

            var productIds = products.Select(p => p.ProductId).ToList();

            var behaviorDict = behaviors
                .GroupBy(b => b.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => new BehaviorScore
                    {
                        Viewed = g.Count(x => x.Viewed == true),
                        Clicked = g.Count(x => x.Clicked == true),
                        Purchased = g.Count(x => x.Purchased == true)
                    });

            var ratingsDict = ratings
                .GroupBy(r => r.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Average(x => x.RatingValue));

            var userBehaviorProducts = behaviors
                .Where(b => b.UserId == userId)
                .Select(b => b.ProductId)
                .Distinct()
                .ToHashSet();

            var userRatedProducts = ratings
                .Where(r => r.UserId == userId)
                .GroupBy(r => r.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(r => r.RatingId).First().RatingValue);

            var userPreferredCategories = behaviors
                .Where(b =>
                    b.UserId == userId &&
                    (b.Viewed == true || b.Clicked == true || b.Purchased == true))
                .Join(
                    products,
                    b => b.ProductId,
                    p => p.ProductId,
                    (b, p) => p.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .GroupBy(c => c)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key!)
                .Take(3)
                .ToHashSet();

            var productCategoryDict = products
                .Where(p => !string.IsNullOrWhiteSpace(p.Category))
                .ToDictionary(
                    p => p.ProductId,
                    p => p.Category!);

            var (bestProductIds, fitnessScore) = RunGeneticAlgorithm(
                productIds,
                behaviorDict,
                ratingsDict,
                userBehaviorProducts,
                userRatedProducts,
                userPreferredCategories,
                productCategoryDict);

            var recommendedProducts = await BuildViewModels(bestProductIds, userId);

            return (recommendedProducts, Math.Round(fitnessScore, 2));
        }

        private (List<int> Recommendations, double FitnessScore) RunGeneticAlgorithm(
            List<int> productIds,
            Dictionary<int, BehaviorScore> behaviorDict,
            Dictionary<int, double> ratingsDict,
            HashSet<int> userBehaviorProducts,
            Dictionary<int, int> userRatedProducts,
            HashSet<string> userPreferredCategories,
            Dictionary<int, string> productCategoryDict)
        {
            int recSize = Math.Min(RecommendationSize, productIds.Count);

            if (recSize == 0)
                return (new List<int>(), 0);

            var population = new List<List<int>>();

            for (int i = 0; i < PopulationSize; i++)
            {
                population.Add(
                    productIds
                        .OrderBy(_ => _random.Next())
                        .Take(recSize)
                        .ToList());
            }

            List<int> bestIndividual = new();
            double bestFitness = -1;

            for (int generation = 0; generation < Generations; generation++)
            {
                var fitnessScores = population
                    .Select(individual => CalculateFitness(
                        individual,
                        behaviorDict,
                        ratingsDict,
                        userBehaviorProducts,
                        userRatedProducts,
                        userPreferredCategories,
                        productCategoryDict))
                    .ToList();

                int bestIndex = fitnessScores.IndexOf(fitnessScores.Max());

                if (fitnessScores[bestIndex] > bestFitness)
                {
                    bestFitness = fitnessScores[bestIndex];
                    bestIndividual = new List<int>(population[bestIndex]);
                }

                var newPopulation = new List<List<int>>
                {
                    new List<int>(bestIndividual)
                };

                while (newPopulation.Count < PopulationSize)
                {
                    var parent1 = TournamentSelection(
                        population,
                        behaviorDict,
                        ratingsDict,
                        userBehaviorProducts,
                        userRatedProducts,
                        userPreferredCategories,
                        productCategoryDict);

                    var parent2 = TournamentSelection(
                        population,
                        behaviorDict,
                        ratingsDict,
                        userBehaviorProducts,
                        userRatedProducts,
                        userPreferredCategories,
                        productCategoryDict);

                    int crossoverPoint = recSize > 1
                        ? _random.Next(1, recSize)
                        : 1;

                    var child1 = CreateChild(parent1, parent2, crossoverPoint, recSize);
                    var child2 = CreateChild(parent2, parent1, crossoverPoint, recSize);

                    Mutate(child1, productIds);
                    Mutate(child2, productIds);

                    newPopulation.Add(child1);

                    if (newPopulation.Count < PopulationSize)
                        newPopulation.Add(child2);
                }

                population = newPopulation;
            }

            return (bestIndividual, bestFitness);
        }

        private double CalculateFitness(
            List<int> recommendationSet,
            Dictionary<int, BehaviorScore> behaviorDict,
            Dictionary<int, double> ratingsDict,
            HashSet<int> userBehaviorProducts,
            Dictionary<int, int> userRatedProducts,
            HashSet<string> userPreferredCategories,
            Dictionary<int, string> productCategoryDict)
        {
            double total = 0;

            foreach (var productId in recommendationSet)
            {
                behaviorDict.TryGetValue(productId, out var behavior);
                ratingsDict.TryGetValue(productId, out var averageRating);
                userRatedProducts.TryGetValue(productId, out var userRating);
                productCategoryDict.TryGetValue(productId, out var category);

                behavior ??= new BehaviorScore();

                total += behavior.Purchased * 10;
                total += behavior.Clicked * 5;
                total += behavior.Viewed * 1;
                total += averageRating * 2;

                if (userBehaviorProducts.Contains(productId))
                    total += 5;

                if (userRating > 0)
                    total += userRating * 3;

                if (!string.IsNullOrWhiteSpace(category) &&
                    userPreferredCategories.Contains(category))
                {
                    total += 4;
                }
            }

            return total;
        }

        private List<int> TournamentSelection(
            List<List<int>> population,
            Dictionary<int, BehaviorScore> behaviorDict,
            Dictionary<int, double> ratingsDict,
            HashSet<int> userBehaviorProducts,
            Dictionary<int, int> userRatedProducts,
            HashSet<string> userPreferredCategories,
            Dictionary<int, string> productCategoryDict)
        {
            return population
                .OrderBy(_ => _random.Next())
                .Take(3)
                .OrderByDescending(individual => CalculateFitness(
                    individual,
                    behaviorDict,
                    ratingsDict,
                    userBehaviorProducts,
                    userRatedProducts,
                    userPreferredCategories,
                    productCategoryDict))
                .First()
                .ToList();
        }

        private List<int> CreateChild(
            List<int> parentA,
            List<int> parentB,
            int crossoverPoint,
            int recommendationSize)
        {
            var firstPart = parentA
                .Take(crossoverPoint)
                .ToList();

            var secondPart = parentB
                .Where(productId => !firstPart.Contains(productId))
                .ToList();

            return firstPart
                .Concat(secondPart)
                .Take(recommendationSize)
                .ToList();
        }

        private void Mutate(List<int> child, List<int> productIds)
        {
            for (int i = 0; i < child.Count; i++)
            {
                if (_random.NextDouble() < MutationRate)
                {
                    var availableProducts = productIds
                        .Where(productId => !child.Contains(productId))
                        .ToList();

                    if (availableProducts.Any())
                    {
                        child[i] = availableProducts[
                            _random.Next(availableProducts.Count)
                        ];
                    }
                }
            }
        }

        private async Task<List<ProductViewModel>> BuildViewModels(List<int> productIds, int userId)
        {
            var result = new List<ProductViewModel>();

            foreach (var productId in productIds)
            {
                var product = await _db.Products.FindAsync(productId);

                if (product == null)
                    continue;

                var avgRating = await _db.Ratings
                    .Where(r => r.ProductId == productId)
                    .AverageAsync(r => (double?)r.RatingValue) ?? 0;

                var purchases = await _db.Behaviors
                    .CountAsync(b => b.ProductId == productId && b.Purchased == true);

                var views = await _db.Behaviors
                    .CountAsync(b => b.ProductId == productId && b.Viewed == true);

                var userRating = await _db.Ratings
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.ProductId == productId);

                var userBehavior = await _db.Behaviors
                    .FirstOrDefaultAsync(b => b.UserId == userId && b.ProductId == productId);

                result.Add(new ProductViewModel
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

            return result;
        }

        public async Task TrackBehaviorAsync(int userId, int productId, string action)
        {
            var behavior = await _db.Behaviors
                .FirstOrDefaultAsync(b => b.UserId == userId && b.ProductId == productId);

            if (behavior == null)
            {
                behavior = new Behavior
                {
                    UserId = userId,
                    ProductId = productId
                };

                _db.Behaviors.Add(behavior);
            }

            switch (action.ToLower())
            {
                case "view":
                    behavior.Viewed = true;
                    break;

                case "click":
                    behavior.Clicked = true;
                    break;

                case "purchase":
                    behavior.Purchased = true;
                    break;
            }

            await _db.SaveChangesAsync();
        }

        public async Task SaveRatingAsync(int userId, int productId, int rating)
        {
            var existingRating = await _db.Ratings
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ProductId == productId);

            if (existingRating != null)
            {
                existingRating.RatingValue = rating;
            }
            else
            {
                _db.Ratings.Add(new Rating
                {
                    UserId = userId,
                    ProductId = productId,
                    RatingValue = rating
                });
            }

            await _db.SaveChangesAsync();
        }

        private class BehaviorScore
        {
            public int Viewed { get; set; }
            public int Clicked { get; set; }
            public int Purchased { get; set; }
        }
    }
}
