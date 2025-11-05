using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClipFunc.DataContext.Models;

public class AccessTokenModel : BaseModel
{
    [Required, Key, StringLength(30)] public required string AccessToken { get; set; }
    [Required] public required DateTime Expires { get; set; }
    public bool IsExpired { get; set; }
}

public class AccessTokenModelConfiguration : IEntityTypeConfiguration<AccessTokenModel>
{
    public void Configure(EntityTypeBuilder<AccessTokenModel> builder)
    {
        builder.UseTpcMappingStrategy()
            .ToTable("AccessTokens")
            .HasKey(x => x.AccessToken);

        builder
            .Property(x => x.AccessToken)
            .HasMaxLength(30)
            .IsRequired();

        builder
            .Property(x => x.Expires)
            .IsRequired();
    }
}