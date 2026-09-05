using Application.Common.Interfaces;
using Application.DTOs.Identity;
using Domain.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Ecommerce.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ITokenService _tokenService;

        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            ITokenService tokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email!);

            if (user == null) return Unauthorized();

            var refreshToken = user.RefreshTokens
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.ExpiresOn)
                .FirstOrDefault();

            return new UserDto
            {
                Email = user.Email!,
                Token = await _tokenService.CreateTokenAsync(user),
                RefreshToken = refreshToken?.Token,
                RefreshTokenExpiration = refreshToken?.ExpiresOn ?? DateTime.MinValue,
                DisplayName = user.DisplayName
            };
        }

        [HttpGet("emailexists")]
        public async Task<ActionResult<bool>> CheckEmailExistsAsync([FromQuery] string email)
        {
            return await _userManager.FindByEmailAsync(email) != null;
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null) return Unauthorized("Invalid email or password");

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
            if (!result.Succeeded) return Unauthorized("Invalid email or password");

            var refreshToken = _tokenService.GenerateRefreshToken();
            user.RefreshTokens.Add(refreshToken);
            RemoveInactiveRefreshTokens(user);
            await _userManager.UpdateAsync(user);

            return new UserDto
            {
                Email = user.Email!,
                Token = await _tokenService.CreateTokenAsync(user),
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiration = refreshToken.ExpiresOn,
                DisplayName = user.DisplayName
            };
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            if (await _userManager.FindByEmailAsync(registerDto.Email) != null)
            {
                return BadRequest("Email address is already in use");
            }

            var user = new AppUser
            {
                DisplayName = registerDto.DisplayName,
                Email = registerDto.Email,
                UserName = registerDto.Email
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded) return BadRequest(result.Errors);

            // إعطاء دور "User" افتراضياً للمستخدم الجديد
            await _userManager.AddToRoleAsync(user, "User");

            var refreshToken = _tokenService.GenerateRefreshToken();
            user.RefreshTokens.Add(refreshToken);
            RemoveInactiveRefreshTokens(user);
            await _userManager.UpdateAsync(user);

            return new UserDto
            {
                DisplayName = user.DisplayName,
                Token = await _tokenService.CreateTokenAsync(user),
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiration = refreshToken.ExpiresOn,
                Email = user.Email
            };
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<UserDto>> RefreshTokenAsync([FromBody] RefreshTokenRequestDto dto)
        {
            var user = await _userManager.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == dto.RefreshToken));

            if (user == null)
                return BadRequest("Invalid Refresh Token");

            var refreshToken = user.RefreshTokens.Single(t => t.Token == dto.RefreshToken);

            if (!refreshToken.IsActive)
                return BadRequest("Refresh Token is inactive or expired");

            refreshToken.RevokedOn = DateTime.UtcNow;

            var newRefreshToken = _tokenService.GenerateRefreshToken();
            user.RefreshTokens.Add(newRefreshToken);
            RemoveInactiveRefreshTokens(user);
            await _userManager.UpdateAsync(user);

            var newJwtToken = await _tokenService.CreateTokenAsync(user);

            return Ok(new UserDto
            {
                DisplayName = user.DisplayName,
                Email = user.Email!,
                Token = newJwtToken,
                RefreshToken = newRefreshToken.Token,
                RefreshTokenExpiration = newRefreshToken.ExpiresOn
            });
        }

        [HttpPost("revoke-token")]
        [Authorize]
        public async Task<IActionResult> RevokeTokenAsync([FromBody] string token)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var user = await _userManager.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return Unauthorized();

            var refreshToken = user.RefreshTokens.SingleOrDefault(t => t.Token == token);
            if (refreshToken == null) return NotFound("Token not found");
            if (!refreshToken.IsActive) return BadRequest("Token is already inactive");

            refreshToken.RevokedOn = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return Ok(new { message = "Token revoked successfully" });
        }
        private static void RemoveInactiveRefreshTokens(AppUser user)
        {
            var cutoff = DateTime.UtcNow.AddDays(-30);
            var staleTokens = user.RefreshTokens
                .Where(t => (t.RevokedOn.HasValue && t.RevokedOn.Value < cutoff) || t.ExpiresOn < cutoff)
                .ToList();

            foreach (var token in staleTokens)
                user.RefreshTokens.Remove(token);
        }
    }
}