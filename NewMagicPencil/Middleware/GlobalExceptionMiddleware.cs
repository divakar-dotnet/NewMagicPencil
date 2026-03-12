using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NewMagicPencil.Data;
using NewMagicPencil.Models;
using System;
using System.Threading.Tasks;

namespace NewMagicPencil.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IServiceScopeFactory scopeFactory)
        {
            _next = next;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                await LogErrorToDatabase(context, ex);
                await RedirectToErrorPage(context, ex);
            }
        }

        private async Task LogErrorToDatabase(HttpContext context, Exception ex)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var pageName = context.Request.Path.Value?.TrimStart('/') ?? "Unknown";

                db.ErrorLogs.Add(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    PageName = pageName.Length > 300 ? pageName.Substring(0, 300) : pageName,
                    AddedDate = DateTime.Now
                });

                await db.SaveChangesAsync();
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Failed to save error to database.");
            }
        }

        private static async Task RedirectToErrorPage(HttpContext context, Exception ex)
        {
            if (!context.Response.HasStarted)
            {
                // Pass error message to the error page via query string
                var encodedMessage = Uri.EscapeDataString(ex.Message ?? "An unexpected error occurred.");
                var encodedPage = Uri.EscapeDataString(context.Request.Path.Value ?? "/");
                context.Response.Redirect($"/Error/Index?message={encodedMessage}&page={encodedPage}");
            }
        }
    }
}