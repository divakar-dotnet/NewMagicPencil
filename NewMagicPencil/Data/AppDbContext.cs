using Microsoft.EntityFrameworkCore;
using NewMagicPencil.Models;

namespace NewMagicPencil.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<CategoryImage> CategoryImages { get; set; }

        public DbSet<GalleryImage> GalleryImages { get; set; }
        public DbSet<ErrorLog> ErrorLogs { get; set; }
    }
}