using NetForge.Core.Models;
using System.Text;
using System.Linq;

namespace NetForge.Core.Utils;

public static class PromptTemplates
{
    public static string BuildReceiptExtractionPrompt()
    {
        var categories = string.Join(", ", ExpenseCategories.Definitions.Keys);
        var subcategories = string.Join(", ", ExpenseCategories.Definitions
            .SelectMany(pair => pair.Value)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase));

        var builder = new StringBuilder();
        builder.AppendLine("Please give me the following information and put in csv format.");
        builder.AppendLine("1. Purchase date (The date could be in various formats. however, the year must be 2025, and please use the format MM/DD/YYYY),");
        builder.AppendLine("2. Merchant/Vendor (Please use carmel case, e.g. NoFrills, Walmart, Costco),");
        builder.AppendLine("3. Item Name,");
        builder.AppendLine("4. Item quantity (integer if there is no item unit and default is 1),");
        builder.AppendLine("5. Item unit (if any, e.g. kg, g, lb. If there is no item unit, please leave it blank),");
        builder.AppendLine("6. Item Price,");
        builder.AppendLine("7. Item Average price,");
        builder.AppendLine($"8. Item category (available categories: {categories}),");
        builder.AppendLine($"9. Item Subcategory (available subcategories: {subcategories})");
        builder.Append("The output must be in a valid CSV format with 9 columns.");

        return builder.ToString();
    }    
}