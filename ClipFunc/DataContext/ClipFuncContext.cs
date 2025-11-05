using ClipFunc.DataContext.Models;
using Microsoft.EntityFrameworkCore;

namespace ClipFunc.DataContext;

public class ClipFuncContext : DbContext
{
    public DbSet<UserModel> Users { get; set; }
    public DbSet<GameModel> Games { get; set; }
    public DbSet<ClipModel> Clips { get; set; }
    public DbSet<AccessTokenModel> AccessTokens { get; set; }

    public ClipFuncContext(DbContextOptions<ClipFuncContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new UserModelConfiguration());
        modelBuilder.ApplyConfiguration(new GameModelConfiguration());
        modelBuilder.ApplyConfiguration(new ClipModelConfiguration());
        modelBuilder.ApplyConfiguration(new AccessTokenModelConfiguration());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries();
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            switch (entry)
            {
                case { State: EntityState.Added, Entity: BaseModel added }:
                    added.CreatedOn = now;
                    added.UpdatedOn = now;
                    break;
                case { State: EntityState.Modified, Entity: BaseModel updated }:
                    updated.UpdatedOn = now;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}