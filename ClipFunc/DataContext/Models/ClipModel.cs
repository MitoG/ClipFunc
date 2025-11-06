using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClipFunc.DataContext.Models;

public class ClipModel : BaseModel
{
    [Key, Required, StringLength(512)] public required string ClipId { get; set; }
    [Required, StringLength(512)] public required string Title { get; set; }
    [Required, StringLength(512)] public required string BroadcasterId { get; set; }
    [Required, StringLength(512)] public required string CreatorId { get; set; }
    [Required, StringLength(512)] public required string GameId { get; set; }
    [Required, StringLength(512)] public required string Url { get; set; }
    [Required, StringLength(512)] public required string ThumbnailUrl { get; set; }
    [Required] public required DateTime ClipCreationDate { get; set; }
    [Required] public required int ViewCount { get; set; }
    [Required] public required double Duration { get; set; }
    public int? VodOffset { get; set; }
    public UserModel? Creator { get; set; }
    public UserModel? Broadcaster { get; set; }
    public GameModel? Game { get; set; }
}

public class ClipModelConfiguration : IEntityTypeConfiguration<ClipModel>
{
    public void Configure(EntityTypeBuilder<ClipModel> builder)
    {
        builder.UseTpcMappingStrategy()
            .ToTable("Clips")
            .HasKey(x => x.ClipId);

        builder
            .HasOne(x => x.Creator)
            .WithMany(x => x.CreatedClips)
            .HasForeignKey(x => x.CreatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Broadcaster)
            .WithMany(x => x.ClipsAsBroadcaster)
            .HasForeignKey(x => x.BroadcasterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Game)
            .WithMany(x => x.Clips)
            .HasForeignKey(x => x.GameId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}