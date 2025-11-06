using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClipFunc.DataContext.Models;

public class UserModel : BaseModel
{
    [Key, Required, StringLength(512)] public required string UserId { get; set; }
    [Required, StringLength(512)] public required string Username { get; set; }
    [Required, StringLength(512)] public required string ProfileImageUrl { get; set; }

    public ICollection<ClipModel> ClipsAsBroadcaster { get; set; } = new List<ClipModel>();
    public ICollection<ClipModel> CreatedClips { get; set; } = new List<ClipModel>();
}

public class UserModelConfiguration : IEntityTypeConfiguration<UserModel>
{
    public void Configure(EntityTypeBuilder<UserModel> builder)
    {
        builder.UseTpcMappingStrategy()
            .ToTable("Users")
            .HasKey(x => x.UserId);
    }
}