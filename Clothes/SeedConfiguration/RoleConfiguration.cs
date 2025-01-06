using Clothes.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clothes.SeedConfiguration;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasData(
            new Role
            {
                Id = "a22df774-3bdf-4222-a3ef-827f4bd5e98a",
                Name = "Visitor",
                NormalizedName = "VISITOR",
                Description = "The customer who only shops"
            },
            new Role
            {
                Id = "475bbb02-c697-4cd1-bf30-9c541017df9e",
                Name = "Admin",
                NormalizedName = "ADMIN",
                Description = "The admin, who adds clothes to the website"
            }
        );
    }
}