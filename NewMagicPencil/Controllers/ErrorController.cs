using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewMagicPencil.Data;
using NewMagicPencil.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NewMagicPencil.Controllers
{
    public class ErrorController : Controller
    {
        private readonly AppDbContext _db;
        public ErrorController(AppDbContext db) { _db = db; }

        // ── Common error page — shown to users when any error happens ─────────
        [AllowAnonymous]
        public IActionResult Index(string message, string page)
        {
            ViewBag.ErrorMessage = message ?? "An unexpected error occurred.";
            ViewBag.PageName = page ?? "Unknown";
            ViewBag.ErrorTime = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
            return View();
        }

        // ── Admin: View all error logs ─────────────────────────────────────────
        [Authorize]
        public async Task<IActionResult> Logs(string search = "", int page = 1)
        {
            int pageSize = 20;
            var query = _db.ErrorLogs.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(e =>
                    e.PageName.Contains(search) ||
                    e.ErrorMessage.Contains(search));

            var totalCount = await query.CountAsync();
            var logs = await query
                .OrderByDescending(e => e.AddedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalCount = totalCount;
            ViewBag.TodayCount = await _db.ErrorLogs.CountAsync(e => e.AddedDate.Date == DateTime.Today);
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.SearchTerm = search;

            return View(logs);
        }

        // ── Admin: Delete one log ──────────────────────────────────────────────
        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var log = await _db.ErrorLogs.FindAsync(id);
            if (log == null) return NotFound();
            _db.ErrorLogs.Remove(log);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Error log deleted.";
            return RedirectToAction("Logs");
        }

        // ── Admin: Delete all logs ─────────────────────────────────────────────
        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAll()
        {
            _db.ErrorLogs.RemoveRange(_db.ErrorLogs);
            await _db.SaveChangesAsync();
            TempData["Success"] = "All error logs cleared.";
            return RedirectToAction("Logs");
        }
    }
}