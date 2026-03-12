using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewMagicPencil.Data;

namespace NewMagicPencil.Controllers
{
    [Authorize]
    public class GalleryModuleController : Controller
    {
        private readonly AppDbContext _db;

        public GalleryModuleController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var images = await _db.GalleryImages
                .Where(g => g.IsLiked)
                .Include(g => g.Category)
                .OrderByDescending(g => g.AddedOn)
                .ToListAsync();

            return View(images);
        }
    }
}