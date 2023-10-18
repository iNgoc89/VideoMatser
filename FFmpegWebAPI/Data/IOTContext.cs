using FFmpegWebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FFmpegWebAPI.Data
{
    public partial class IOTContext : DbContext
    {
        protected IOTContext()
        {
        }
        public IOTContext(DbContextOptions options) : base(options)
        {
        }
        public virtual DbSet<Camera> Camera { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:DefaultSchema", "cmrs");
          
            OnModelCreatingPartial(modelBuilder);
        }
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
