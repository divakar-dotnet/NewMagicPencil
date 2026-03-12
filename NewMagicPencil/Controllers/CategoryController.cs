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

        // ── CATEGORY CRUD ──────────────────────────────────────────────────

        public async Task<IActionResult> Index()
        {
            var categories = await _db.Categories
                .OrderByDescending(c => c.CreatedOn).ToListAsync();
            return View(categories);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.AllCategories = await _db.Categories
                .OrderByDescending(c => c.CreatedOn).ToListAsync();
            return View(new CategoryFormModel());
        }

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
                Status = "Active",          // always Active by default
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
                .AnyAsync(c => c.CategoryName.ToLower() == model.CategoryName.ToLower()
                            && c.Id != id);
            if (exists)
            {
                ModelState.AddModelError("", "A category with this name already exists.");
                ViewBag.CategoryId = id;
                return View(model);
            }

            category.CategoryName = model.CategoryName.Trim();
            category.UpdatedOn = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Category updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ── HARD DELETE ────────────────────────────────────────────────────

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Delete(int id)
        //{
        //    var category = await _db.Categories
        //        .Include(c => c.CategoryImages)
        //        .FirstOrDefaultAsync(c => c.Id == id);

        //    if (category == null) return NotFound();

        //    // Delete physical image files
        //    foreach (var img in category.CategoryImages)
        //    {
        //        if (!string.IsNullOrEmpty(img.ImageUrl))
        //        {
        //            var root = _env.WebRootPath
        //                       ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        //            var path = Path.Combine(root,
        //                img.ImageUrl.TrimStart('/')
        //                            .Replace("/", Path.DirectorySeparatorChar.ToString()));
        //            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        //        }
        //    }

        //    _db.Categories.Remove(category);
        //    await _db.SaveChangesAsync();
        //    TempData["Success"] = "Category deleted permanently.";
        //    return RedirectToAction(nameof(Index));
        //}

        // ── TOGGLE STATUS (AJAX) ───────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var category = await _db.Categories.FindAsync(id);
            if (category == null)
                return Json(new { success = false, message = "Category not found." });

            category.Status = category.Status == "Active" ? "Inactive" : "Active";
            category.UpdatedOn = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Json(new { success = true, newStatus = category.Status });
        }

        // ── IMAGE UPLOAD ───────────────────────────────────────────────────

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
                .OrderBy(ci => ci.SortOrder)
                .ThenByDescending(ci => ci.UploadedOn)
                .ToListAsync();

            return View(images);
        }

        //public async Task<IActionResult> Upload(int categoryId)
        //{
        //    var category = await _db.Categories.FindAsync(categoryId);
        //    if (category == null)
        //    {
        //        TempData["Error"] = "Category not found.";
        //        return RedirectToAction(nameof(Index));
        //    }

        //    // If inactive — redirect to blocked page
        //    if (category.Status == "Inactive")
        //    {
        //        ViewBag.CategoryName = category.CategoryName;
        //        ViewBag.CategoryId = categoryId;
        //        return View("UploadBlocked");
        //    }

        //    ViewBag.CategoryId = categoryId;
        //    ViewBag.CategoryName = category.CategoryName;

        //    var images = await _db.CategoryImages
        //        .Where(ci => ci.CategoryId == categoryId)
        //        .OrderBy(ci => ci.SortOrder)
        //        .ThenByDescending(ci => ci.UploadedOn)
        //        .ToListAsync();

        //    return View(images);
        //}





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
                    TempData["Error"] = $"'{img.FileName}' is not allowed. Only JPG, JPEG, PNG.";
                    return RedirectToAction(nameof(Upload), new { categoryId });
                }
            }

            var root = _env.WebRootPath
                       ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var dir = Path.Combine(root, "uploads", "portfolio-images");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var maxSort = await _db.CategoryImages
                .Where(ci => ci.CategoryId == categoryId)
                .MaxAsync(ci => (int?)ci.SortOrder) ?? -1;

            foreach (var img in images)
            {
                maxSort++;
                var fileName = Guid.NewGuid() + Path.GetExtension(img.FileName).ToLower();
                using var stream = new FileStream(Path.Combine(dir, fileName), FileMode.Create);
                await img.CopyToAsync(stream);

                _db.CategoryImages.Add(new CategoryImage
                {
                    CategoryId = categoryId,
                    ImageUrl = "/uploads/portfolio-images/" + fileName,
                    Description = description?.Trim(),
                    Status = status ?? "Active",
                    SortOrder = maxSort,
                    UploadedOn = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"{images.Count} image(s) uploaded successfully!";
            return RedirectToAction(nameof(Upload), new { categoryId });
        }

        // ── EDIT IMAGE DESCRIPTION (AJAX) ──────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> EditImage(int id, string description)
        {
            var image = await _db.CategoryImages.FindAsync(id);
            if (image == null)
                return Json(new { success = false, message = "Image not found." });

            image.Description = description?.Trim();
            image.UpdatedOn = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Json(new { success = true, message = "Description updated.", description = image.Description });
        }

        // ── REORDER IMAGES (AJAX) ──────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> ReorderImages([FromBody] List<ReorderDto> order)
        {
            if (order == null || order.Count == 0)
                return Json(new { success = false, message = "No order data received." });

            foreach (var item in order)
            {
                var image = await _db.CategoryImages.FindAsync(item.Id);
                if (image != null)
                {
                    image.SortOrder = item.SortOrder;
                    image.UpdatedOn = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync();
            return Json(new { success = true, message = "Order saved successfully." });
        }

        // ── DELETE IMAGE (AJAX) ────────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> DeleteImage(int id, int categoryId)
        {
            var image = await _db.CategoryImages.FindAsync(id);
            if (image == null)
                return Json(new { success = false, message = "Image not found." });

            if (!string.IsNullOrEmpty(image.ImageUrl))
            {
                var root = _env.WebRootPath
                           ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var path = Path.Combine(root,
                    image.ImageUrl.TrimStart('/')
                                  .Replace("/", Path.DirectorySeparatorChar.ToString()));
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }

            _db.CategoryImages.Remove(image);
            await _db.SaveChangesAsync();
            return Json(new { success = true, message = "Image deleted successfully." });
        }

        // ── REPLACE IMAGE (AJAX) ───────────────────────────────────────────

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

            var root = _env.WebRootPath
                       ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            if (!string.IsNullOrEmpty(image.ImageUrl))
            {
                var oldPath = Path.Combine(root,
                    image.ImageUrl.TrimStart('/')
                                  .Replace("/", Path.DirectorySeparatorChar.ToString()));
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





        // ── GALLERY PAGE ───────────────────────────────────────────────────

        public async Task<IActionResult> Gallery(int categoryId)
        {
            var category = await _db.Categories.FindAsync(categoryId);
            if (category == null)
            {
                TempData["Error"] = "Category not found.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryName = category.CategoryName;

            var images = await _db.GalleryImages
                .Where(g => g.CategoryId == categoryId)
                .OrderBy(g => g.SortOrder)
                .ThenByDescending(g => g.AddedOn)
                .ToListAsync();

            return View(images);
        }

        // ── ADD TO GALLERY (AJAX) ──────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> AddToGallery(int categoryImageId, int categoryId)
        {
            var source = await _db.CategoryImages.FindAsync(categoryImageId);
            if (source == null)
                return Json(new { success = false, message = "Image not found." });

            // Prevent duplicates
            var already = await _db.GalleryImages
                .AnyAsync(g => g.CategoryImageId == categoryImageId && g.CategoryId == categoryId);
            if (already)
                return Json(new { success = true, alreadyAdded = true, message = "Already in gallery." });

            var maxSort = await _db.GalleryImages
                .Where(g => g.CategoryId == categoryId)
                .MaxAsync(g => (int?)g.SortOrder) ?? -1;

            _db.GalleryImages.Add(new GalleryImage
            {
                CategoryId = categoryId,
                CategoryImageId = categoryImageId,
                ImageUrl = source.ImageUrl,
                Description = source.Description,
                SortOrder = maxSort + 1,
                AddedOn = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            var count = await _db.GalleryImages.CountAsync(g => g.CategoryId == categoryId);
            return Json(new { success = true, alreadyAdded = false, message = "Added to gallery!", galleryCount = count });
        }

        // ── GALLERY COUNT (AJAX) ───────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GalleryCount(int categoryId)
        {
            var count = await _db.GalleryImages.CountAsync(g => g.CategoryId == categoryId);
            return Json(new { count });
        }






        // ── TOGGLE LIKE (AJAX) ─────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ToggleLike(int id)
        {
            var image = await _db.GalleryImages.FindAsync(id);
            if (image == null)
                return Json(new { success = false });

            image.IsLiked = !image.IsLiked;
            await _db.SaveChangesAsync();

            return Json(new { success = true, isLiked = image.IsLiked });
        }





        // ── DELETE from GalleryImages only (keeps original CategoryImage intact) ──
        //[HttpPost]
        //public async Task<IActionResult> DeleteFromGallery(int id)
        //{
        //    try
        //    {
        //        var gallery = await _db.GalleryImages.FindAsync(id);
        //        if (gallery == null)
        //            return Json(new { success = false, message = "Not found" });

        //        _db.GalleryImages.Remove(gallery);
        //        await _db.SaveChangesAsync();

        //        return Json(new { success = true });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = ex.Message });
        //    }
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string returnUrl = "Index")
        {
            var category = await _db.Categories
                .Include(c => c.CategoryImages)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            // Delete physical image files
            foreach (var img in category.CategoryImages)
            {
                if (!string.IsNullOrEmpty(img.ImageUrl))
                {
                    var root = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                    var path = Path.Combine(root, img.ImageUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Category deleted permanently.";

            // Redirect back to whichever page called the delete (Create or Index)
            return RedirectToAction(returnUrl);
        }





    }

    public class CategoryFormModel
    {
        [Required(ErrorMessage = "Category Name is required.")]
        [StringLength(200)]
        public string CategoryName { get; set; }
        public string Status { get; set; } = "Active";
    }

    public class ReorderDto
    {
        public int Id { get; set; }
        public int SortOrder { get; set; }
    }
}