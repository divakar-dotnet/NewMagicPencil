using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewMagicPencil.Data;
using NewMagicPencil.Models;
using System.ComponentModel.DataAnnotations;

namespace NewMagicPencil.Controllers
{
    [Authorize]
    public class BlogController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public BlogController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        //public async Task<IActionResult> Index()
        //{
        //    var blogs = await _db.Blogs.OrderByDescending(b => b.PostedDate).ToListAsync();
        //    return View(blogs);
        //}
        // ── INDEX: Shows blogs sorted Alphabetically by Title ──
        public async Task<IActionResult> Index()
        {
            var blogs = await _db.Blogs
                .OrderBy(b => b.Title) // Updated to sort A-Z
                .ToListAsync();
            return View(blogs);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlogFormModel model)
        {
            if (!ModelState.IsValid) return View(model);

            string imagePath = null;
            if (model.Image != null)
                imagePath = await SaveImage(model.Image, "blog-images");

            var blog = new Blog
            {
                Title = model.Title,
                ShortDescription = model.ShortDescription,
                Content = model.Content,
                Hashtags = model.Hashtags,
                ImageUrl = imagePath,
                PostedDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity.Name
            };

            _db.Blogs.Add(blog);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Blog created successfully!";
            return RedirectToAction(nameof(Index));
        }

        //public async Task<IActionResult> Edit(int id)
        //{
        //    var blog = await _db.Blogs.FindAsync(id);
        //    if (blog == null) return NotFound();

        //    var model = new BlogFormModel
        //    {
        //        Title = blog.Title,
        //        ShortDescription = blog.ShortDescription,
        //        Content = blog.Content,
        //        Hashtags = blog.Hashtags
        //    };
        //    ViewBag.ExistingImage = blog.ImageUrl;
        //    return View(model);
        //}// GET: Blog/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var blog = await _db.Blogs.FindAsync(id);
            if (blog == null) return NotFound();

            var model = new BlogFormModel
            {
                Title = blog.Title,
                ShortDescription = blog.ShortDescription,
                Content = blog.Content,
                Hashtags = blog.Hashtags
            };
            ViewBag.ExistingImage = blog.ImageUrl;
            ViewBag.BlogId = id;

            return View(model);
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id, BlogFormModel model)
        //{
        //    if (!ModelState.IsValid) return View(model);

        //    var blog = await _db.Blogs.FindAsync(id);
        //    if (blog == null) return NotFound();

        //    if (model.Image != null)
        //    {
        //        DeleteImage(blog.ImageUrl);
        //        blog.ImageUrl = await SaveImage(model.Image, "blog-images");
        //    }

        //    blog.Title = model.Title;
        //    blog.ShortDescription = model.ShortDescription;
        //    blog.Content = model.Content;
        //    blog.Hashtags = model.Hashtags;
        //    blog.UpdatedAt = DateTime.UtcNow;

        //    await _db.SaveChangesAsync();
        //    TempData["Success"] = "Blog updated successfully!";
        //    return RedirectToAction(nameof(Index));
        //}


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BlogFormModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var blog = await _db.Blogs.FindAsync(id);
            if (blog == null) return NotFound();

            if (model.Image != null)
            {
                // Delete old image if new one is uploaded
                if (!string.IsNullOrEmpty(blog.ImageUrl))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, blog.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }
                blog.ImageUrl = await SaveImage(model.Image, "blog-images");
            }

            blog.Title = model.Title;
            blog.ShortDescription = model.ShortDescription;
            blog.Content = model.Content;
            blog.Hashtags = model.Hashtags;
            blog.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Blog updated successfully!";
            return RedirectToAction(nameof(Index));
        }




        public async Task<IActionResult> Details(int id)
        {
            var blog = await _db.Blogs.FindAsync(id);
            if (blog == null) return NotFound();
            return View(blog);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var blog = await _db.Blogs.FindAsync(id);
            if (blog == null) return NotFound();

            DeleteImage(blog.ImageUrl);
            _db.Blogs.Remove(blog);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Blog deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveImage(IFormFile file, string folder)
        {
            // Use ContentRootPath + wwwroot as fallback — more reliable
            var root = _env.WebRootPath;

            if (string.IsNullOrEmpty(root))
                root = Path.Combine(_env.ContentRootPath, "wwwroot");

            var dir = Path.Combine(root, "uploads", folder);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName).ToLower();
            var fullPath = Path.Combine(dir, fileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/{folder}/{fileName}";
        }

        private void DeleteImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;
            var root = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var path = Path.Combine(root, imageUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
    }

    public class BlogFormModel
    {
        [Required] public string Title { get; set; }
        [Required] public string ShortDescription { get; set; }
        [Required] public string Content { get; set; }
        public IFormFile? Image { get; set; }
        public string? Hashtags { get; set; }
    }
}   