using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DigitalEvidenceSystem.Web.Data;
using DigitalEvidenceSystem.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace DigitalEvidenceSystem.Web.Pages.Account;
public sealed class LoginModel(EvidenceDbContext db, IPasswordHasher<AppUser> hasher) : PageModel { [BindProperty] public LoginInput Input { get; set; } = new(); public sealed class LoginInput { [Required] public string Username { get; set; } = ""; [Required, DataType(DataType.Password)] public string Password { get; set; } = ""; public bool RememberMe { get; set; } } public async Task<IActionResult> OnPostAsync(string? returnUrl = null) { if (!ModelState.IsValid) return Page(); var user = await db.Users.SingleOrDefaultAsync(u => u.Username == Input.Username.Trim() && u.IsActive); if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, Input.Password) == PasswordVerificationResult.Failed) { ModelState.AddModelError(string.Empty, "The username or password is incorrect."); return Page(); } var claims = new[] { new Claim(ClaimTypes.Name, user.FullName), new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Role, user.Role) }; user.LastLoginAt = DateTime.Now; await db.SaveChangesAsync(); await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)), new AuthenticationProperties { IsPersistent = Input.RememberMe, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) }); return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl) ? "/" : returnUrl); } }
