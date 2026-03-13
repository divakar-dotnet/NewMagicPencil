//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using NewMagicPencil.Data;
//using Microsoft.EntityFrameworkCore;

//namespace NewMagicPencil.Controllers
//{
//    [Authorize]
//    public class DashboardController : Controller
//    {
//        private readonly AppDbContext _db;

//        public DashboardController(AppDbContext db)
//        {
//            _db = db;
//        }

//        public async Task<IActionResult> Index()
//        {
//            try { ViewBag.BlogCount = await _db.Blogs.CountAsync(); }
//            catch { ViewBag.BlogCount = 0; }

//            try { ViewBag.CategoryCount = await _db.Categories.CountAsync(); }
//            catch { ViewBag.CategoryCount = 0; }

//            try { ViewBag.GalleryCount = await _db.GalleryImages.Where(g => g.IsLiked).CountAsync(); }
//            catch { ViewBag.GalleryCount = 0; }

//            ViewBag.UserEmail = User.Identity?.Name ?? "Admin";
//            //throw new Exception("Test error from Dashboard page");

//            return View();
//        }
//    }
//}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewMagicPencil.Data;
using Microsoft.EntityFrameworkCore;

namespace NewMagicPencil.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _db;

        public DashboardController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Count Total Blogs
            try { ViewBag.BlogCount = await _db.Blogs.CountAsync(); }
            catch { ViewBag.BlogCount = 0; }

            // 2. Count ONLY Active Categories for Portfolio
            try
            {
                ViewBag.CategoryCount = await _db.Categories
                    .Where(c => c.Status == "Active")
                    .CountAsync();
            }
            catch { ViewBag.CategoryCount = 0; }

            // 3. Count Liked Gallery Images
            try { ViewBag.GalleryCount = await _db.GalleryImages.Where(g => g.IsLiked).CountAsync(); }
            catch { ViewBag.GalleryCount = 0; }

            ViewBag.UserEmail = User.Identity?.Name ?? "Admin";

            return View();
        }
    }
}