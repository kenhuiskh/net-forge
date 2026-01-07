using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetForge.Core.Models;

namespace NetForge.Core.Configuration;

public class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired();
            
        // Self-referencing relationship
        builder.HasOne(e => e.Parent)
            .WithMany(e => e.Children)
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.CreatedAt).ValueGeneratedOnAdd();
        builder.Property(e => e.UpdatedAt).ValueGeneratedOnAddOrUpdate();
    }
}