using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewMagicPencil.Data;
using NewMagicPencil.Models;
using System.ComponentModel.DataAnnotations;

namespace NewMagicPencil.Controllers
{
    [Authorize]
    public class CategoryController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public CategoryController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // ── CATEGORY CRUD ─────────────────────────────────────

        public async Task<IActionResult> Index()
        {
            var categories = await _db.Categories.OrderByDescending(c => c.CreatedOn).ToListAsync();
            return View(categories);
        }

        public IActionResult Create() => View(new CategoryFormModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryFormModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var exists = await _db.Categories
                .AnyAsync(c => c.CategoryName.ToLower() == model.CategoryName.ToLower());
            if (exists)
            {
                ModelState.AddModelError("", "A category with this name already exists.");
                return View(model);
            }

            _db.Categories.Add(new Category
            {
                CategoryName = model.CategoryName.Trim(),
                Status = model.Status,
                CreatedOn = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Category created successfully!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var category = await _db.Categories.FindAsync(id);
            if (category == null) return NotFound();

            ViewBag.CategoryId = id;
            return View(new CategoryFormModel
            {
                CategoryName = category.CategoryName,
                Status = category.Status
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryFormModel model)
        {
            if (!ModelState.IsValid) { ViewBag.CategoryId = id; return View(model); }

            var category = await _db.Categories.FindAsync(id);
            if (category == null) return NotFound();

            var exists = await _db.Categories
                .AnyAsync(c => c.CategoryName.ToLower() == model.CategoryName.ToLower() && c.Id != id);
            if (exists)
            {
                ModelState.AddModelError("", "A category with this name already exists.");
                ViewBag.CategoryId = id;
                return View(model);
            }

            category.CategoryName = model.CategoryName.Trim();
            category.Status = model.Status;
            category.UpdatedOn = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Category updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _db.Categories.FindAsync(id);
            if (category == null) return NotFound();

            category.Status = "Inactive";
            category.UpdatedOn = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Category marked as Inactive.";
            return RedirectToAction(nameof(Index));
        }

        // ── IMAGE UPLOAD ──────────────────────────────────────

        public async Task<IActionResult> Upload(int categoryId)
        {
            var category = await _db.Categories.FindAsync(categoryId);
            if (category == null)
            {
                TempData["Error"] = "Category not found.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryName = category.CategoryName;

            var images = await _db.CategoryImages
                .Where(ci => ci.CategoryId == categoryId)
                .OrderByDescending(ci => ci.UploadedOn)
                .ToListAsync();

            return View(images);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int categoryId, List<IFormFile> images,
            string description, string status)
        {
            if (images == null || images.Count == 0)
            {
                TempData["Error"] = "Please select at least one image.";
                return RedirectToAction(nameof(Upload), new { categoryId });
            }

            if (images.Count > 15)
            {
                TempData["Error"] = "Maximum 15 images allowed at a time.";
                return RedirectToAction(nameof(Upload), new { categoryId });
            }

            var allowed = new[] { ".jpg", ".jpeg", ".png" };
            foreach (var img in images)
            {
                var ext = Path.GetExtension(img.FileName).ToLower();
                if (!allowed.Contains(ext))
                {
                    TempData["Error"] = $"'{img.FileName}' is not allowed. Only JPG, JPEG, PNG accepted.";
                    return RedirectToAction(nameof(Upload), new { categoryId });
                }
            }

            var root = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var dir = Path.Combine(root, "uploads", "portfolio-images");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            foreach (var img in images)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(img.FileName).ToLower();
                using var stream = new FileStream(Path.Combine(dir, fileName), FileMode.Create);
                await img.CopyToAsync(stream);

                _db.CategoryImages.Add(new CategoryImage
                {
                    CategoryId = categoryId,
                    ImageUrl = "/uploads/portfolio-images/" + fileName,
                    Description = description?.Trim(),
                    Status = status ?? "Active",
                    UploadedOn = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"{images.Count} image(s) uploaded successfully!";
            return RedirectToAction(nameof(Upload), new { categoryId });
        }

        // ── EDIT IMAGE DESCRIPTION (AJAX) ─────────────────────

        [HttpPost]
        public async Task<IActionResult> EditImage(int id, string description)
        {
            var image = await _db.CategoryImages.FindAsync(id);
            if (image == null)
                return Json(new { success = false, message = "Image not found." });

            image.Description = description?.Trim();
            image.UpdatedOn = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Json(new { success = true, message = "Description updated successfully.", description = image.Description });
        }

        // ── DELETE IMAGE (AJAX) ───────────────────────────────

        [HttpPost]
        public async Task<IActionResult> DeleteImage(int id, int categoryId)
        {
            var image = await _db.CategoryImages.FindAsync(id);
            if (image == null)
                return Json(new { success = false, message = "Image not found." });

            if (!string.IsNullOrEmpty(image.ImageUrl))
            {
                var root = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var path = Path.Combine(root, image.ImageUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }

            _db.CategoryImages.Remove(image);
            await _db.SaveChangesAsync();
            return Json(new { success = true, message = "Image deleted successfully." });
        }

        // ── REPLACE IMAGE (AJAX) ──────────────────────────────

        [HttpPost]
        public async Task<IActionResult> ReplaceImage(int id, int categoryId, IFormFile newImage)
        {
            if (newImage == null)
                return Json(new { success = false, message = "No file selected." });

            var allowed = new[] { ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(newImage.FileName).ToLower();
            if (!allowed.Contains(ext))
                return Json(new { success = false, message = "Only JPG, JPEG, PNG allowed." });

            var image = await _db.CategoryImages.FindAsync(id);
            if (image == null)
                return Json(new { success = false, message = "Image not found." });

            var root = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            if (!string.IsNullOrEmpty(image.ImageUrl))
            {
                var oldPath = Path.Combine(root, image.ImageUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }

            var dir = Path.Combine(root, "uploads", "portfolio-images");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var fileName = Guid.NewGuid() + ext;
            using var stream = new FileStream(Path.Combine(dir, fileName), FileMode.Create);
            await newImage.CopyToAsync(stream);

            image.ImageUrl = "/uploads/portfolio-images/" + fileName;
            image.UpdatedOn = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Json(new { success = true, message = "Image replaced successfully." });
        }
    }

    public class CategoryFormModel
    {
        [Required(ErrorMessage = "Category Name is required.")]
        [StringLength(200)]
        public string CategoryName { get; set; }
        [Required]
        public string Status { get; set; } = "Active";
    }
}