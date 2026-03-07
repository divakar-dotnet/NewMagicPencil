using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewMagicPencil.Data;
using NewMagicPencil.Services;
using System.Security.Claims;

namespace NewMagicPencil.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly JwtService _jwtService;
        private readonly EmailService _emailService;

        public AccountController(AppDbContext db, JwtService jwtService, EmailService emailService)
        {
            _db = db;
            _jwtService = jwtService;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Dashboard");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _db.AdminUsers
                .FirstOrDefaultAsync(x => x.Email == email && x.Password == password);

            if (user != null)
            {
                var token = _jwtService.GenerateToken(user.Email);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, email),
                    new Claim(ClaimTypes.Role, "Admin"),
                    new Claim("JwtToken", token)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = false,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                    });

                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Error = "Access Denied: Invalid Credentials";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> SendOtp([FromBody] string email)
        {
            try
            {
                var user = await _db.AdminUsers.FirstOrDefaultAsync(x => x.Email == email);
                if (user == null)
                    return BadRequest(new { success = false, message = "No account found with this email." });

                var otp = new Random().Next(10000, 99999).ToString();
                user.OtpCode = otp;
                user.OtpExpiry = DateTime.Now.AddMinutes(5);
                await _db.SaveChangesAsync();
                await _emailService.SendOtpEmailAsync(user.Email, otp);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            var user = await _db.AdminUsers.FirstOrDefaultAsync(x => x.Email == dto.Email);
            if (user == null)
                return BadRequest(new { success = false, message = "User not found." });
            if (user.OtpCode != dto.Otp)
                return BadRequest(new { success = false, message = "Invalid OTP." });
            if (user.OtpExpiry < DateTime.Now)
                return BadRequest(new { success = false, message = "OTP has expired." });

            return Ok(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest(new { success = false, message = "Passwords do not match." });
            if (dto.NewPassword.Length < 6)
                return BadRequest(new { success = false, message = "Password must be at least 6 characters." });

            var user = await _db.AdminUsers.FirstOrDefaultAsync(x => x.Email == dto.Email);
            if (user == null)
                return BadRequest(new { success = false, message = "User not found." });
            if (user.OtpCode != dto.Otp)
                return BadRequest(new { success = false, message = "Invalid OTP." });
            if (user.OtpExpiry < DateTime.Now)
                return BadRequest(new { success = false, message = "OTP has expired." });

            user.Password = dto.NewPassword;
            user.OtpCode = null;
            user.OtpExpiry = null;
            await _db.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }

    public class VerifyOtpDto { public string Email { get; set; } public string Otp { get; set; } }
    public class ResetPasswordDto
    {
        public string Email { get; set; }
        public string Otp { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}