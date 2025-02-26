using System.Security.Claims;
using BankBackend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

namespace BankBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly BankContex _context;

        public AccountController(BankContex context)
        {
            _context = context;
        }
        [HttpGet("balance")]
        public async Task<IActionResult> AccountInfo()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized("User not found in token.");
            //find the user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound("User does not exist.");
            }
            return Ok(new {balance = user.Balance });
        }

        [HttpGet("user")]
        public async Task<IActionResult> UserInfo()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized("User not found in token.");
            //find the user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound("User does not exist.");
            }
            return Ok(new
            {
                name = user.Name,
                email = user.Email,
                balance = user.Balance,
            });
        }
    }
}
