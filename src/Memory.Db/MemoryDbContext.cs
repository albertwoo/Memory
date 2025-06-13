namespace Memory.Db;

using Microsoft.EntityFrameworkCore;

public class MemoryDbContext : DbContext {
    private string? designTimePath;

    public MemoryDbContext() : base() {
        designTimePath = "Data Source=Memory.db";
    }

    public MemoryDbContext(DbContextOptions<MemoryDbContext> options) : base(options) { }

    public DbSet<Memory> Memories { get; set; }
    public DbSet<MemoryMeta> MemoryMetas { get; set; }
    public DbSet<MemoryTag> MemoryTags { get; set; }
    public DbSet<Tag> Tags { get; set; }

    protected override void OnModelCreating(ModelBuilder builder) {
        builder.Entity<Memory>().HasKey(x => x.Id);
        
        builder.Entity<Memory>().HasIndex(x => x.CreationTime);
        builder.Entity<Memory>().HasIndex(x => x.FilePathHash);
        builder.Entity<Memory>().HasIndex(x => x.FileContentHash);
        builder.Entity<Memory>().HasIndex(x => x.Year);
        builder.Entity<Memory>().HasIndex(x => x.Month);
        builder.Entity<Memory>().HasIndex(x => x.Day);

        builder.Entity<Memory>().Property(x => x.CreationTime).IsRequired();
        builder.Entity<Memory>().Property(x => x.FilePath).HasMaxLength(300).IsRequired();
        builder.Entity<Memory>().Property(x => x.FilePathHash).HasMaxLength(32).IsRequired();
        
        builder.Entity<Memory>()
            .Property(e => e.Year)
            .HasComputedColumnSql("CAST(strftime('%Y', CreationTime) AS INTEGER)")
            .IsRequired();
        builder.Entity<Memory>()
            .Property(e => e.Month)
            .HasComputedColumnSql("CAST(strftime('%m', CreationTime) AS INTEGER)")
            .IsRequired();
        builder.Entity<Memory>()
            .Property(e => e.Day)
            .HasComputedColumnSql("CAST(strftime('%d', CreationTime) AS INTEGER)")
            .IsRequired();


        builder.Entity<MemoryMeta>().HasKey(x => x.Id);

        builder.Entity<Tag>().HasKey(x => x.Id);
        builder.Entity<Tag>().Property(x => x.Name).IsRequired().HasMaxLength(20);

        builder.Entity<MemoryTag>().HasKey(x => new { x.TagId, x.MemoryId });
        builder
            .Entity<MemoryTag>()
            .HasOne(x => x.Memory)
            .WithMany(x => x.MemoryTags)
            .HasForeignKey(x => x.MemoryId);
        builder
            .Entity<MemoryTag>()
            .HasOne(x => x.Tag)
            .WithMany(x => x.MemoryTags)
            .HasForeignKey(x => x.TagId);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options) {
        if (!options.IsConfigured) {
            options.UseSqlite(designTimePath);
        }
    }
}