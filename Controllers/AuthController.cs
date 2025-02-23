using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.Xml;
using System.Text;
using BankBackend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace BankBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        // Provides methods to create users, find them by email, update passwords
        private readonly UserManager<BankUser> _userManager;
        //responsibale on user login and verify Checks passwords on login, handles lockout, 2FA, etc.
        private readonly SignInManager<BankUser> _signInManager;
        //acsses to app steing in the appsttings json.
        private readonly IConfiguration _config;

        //dont need the context  Identity Services Hide the EF Core Details UserManager, SignInManager use bankDB contex
        public AuthController(
            UserManager<BankUser> userManager,
            SignInManager<BankUser> signInManager,
            IConfiguration config )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
        }
        //post register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {

            //check if exsist
            var userExsist = await _userManager.FindByEmailAsync(model.Email);
            if (userExsist != null)
            {
                return BadRequest("user already exsist");
            }

            var newUser = new BankUser
            {
                UserName = model.Email,
                Email = model.Email,
                Name = model.Name
            };
            //UserManager  create the user with a hashed password
            var result = await _userManager.CreateAsync(newUser, model.Password);
            if (!result.Succeeded)
            {
                //Return error messages if creation failed
                return BadRequest(result.Errors);
            }
            return Created();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            //if user exsist
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return NotFound("user doesnt exsist");

            //check password
            /* 
             * TUser user Identity user, the Paswword to comper
             * lockoutOnFailure false determines whether to lock out the user after a certain number of failed attempts
             * false means do not lock out the account  true meanse lock after few time
             */
            var passwordValid = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!passwordValid.Succeeded)
                return Unauthorized("worng password");

            var token = GenerateJwtToken(user);

            return Ok(new 
            { 
                token,
                name = user.Name,
                balance = user.Balance
            });

        }
        private string GenerateJwtToken(BankUser user)
        {
            //get the secrate key from cofiuge
            var secretKey = _config["JwtSettings:SecretKey"];
            //make the secrete key to valid key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            //create token 
            //Convert the string secretKey into a byte array (using UTF8)
            //Wrap that in a SymmetricSecurityKey object that the JWT library can use to sign the token
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            //data on user 
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
            };
            var token = new JwtSecurityToken(
             claims: claims,
             expires: DateTime.UtcNow.AddHours(1),
             signingCredentials: creds
             );
            //he JwtSecurityTokenHandler can serialize the token into a
            //standard JWT string (Header.Payload.Signature).
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
