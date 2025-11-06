using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClipFunc.DataContext.Models;

public class GameModel : BaseModel
{
    [Key, Required, StringLength(512)] public required string GameId { get; set; }
    [Required, StringLength(512)] public required string Name { get; set; }
    [Required, StringLength(512)] public required string BoxArtUrl { get; set; }
    [StringLength(512)] public string? IgdbId { get; set; }

    public ICollection<ClipModel> Clips { get; set; } = new List<ClipModel>();
}

public class GameModelConfiguration : IEntityTypeConfiguration<GameModel>
{
    public void Configure(EntityTypeBuilder<GameModel> builder)
    {
        builder
            .UseTpcMappingStrategy()
            .ToTable("Games")
            .HasKey(x => x.GameId);
    }
}