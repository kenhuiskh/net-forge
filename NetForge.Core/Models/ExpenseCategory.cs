using NetForge.Core.Interfaces;

namespace NetForge.Core.Models;

public class ExpenseCategory: ITrackable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ExpenseCategory? Parent { get; set; }
    public List<ExpenseCategory> Children { get; set; } = new();
}