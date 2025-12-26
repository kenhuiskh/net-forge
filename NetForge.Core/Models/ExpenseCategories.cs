using System;
using System.Collections.Generic;

namespace NetForge.Core.Models;

public static class ExpenseCategories
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Definitions =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
    {
        ["Food"] = new[]
        {
            "Rice/Dry", "Drinks", "Tins/Frozen", "Breakfast", "Vegetable", "Fruits",
            "Meat", "Sauce", "Dairy", "Snacks", "Seafood"
        },
        ["Household"] = new[] { "Toiletries", "Kitchen", "Others" },
        ["HST"] = Array.Empty<string>(),
        ["Beauty"] = new[] { "Others", "Supplement" },
        ["Dining"] = new[] { "Lunch", "Dinner" },
        ["Pet"] = new[] { "Kibble", "Can", "Medical", "Wet Food", "Litter" },
        ["Other"] = new[] { "Clothes", "Stationary", "House", "Learning", "Entertainment" },
        ["Commute"] = new[] { "Travel", "Car Rental", "Public" }
    };
}