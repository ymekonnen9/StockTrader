using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StockTrader.Application.DTOs;
using StockTrader.Domain.Entities;

namespace StockTrader.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _usermanager;
        private readonly RoleManager<IdentityRole> _rolemanager;
        private readonly SignInManager<ApplicationUser> _signinmanager;
    
        public AccountsController(UserManager<ApplicationUser> usermanager, RoleManager<IdentityRole> rolemanager, SignInManager<ApplicationUser> signinmanager)
        {
            _usermanager = usermanager;
            _rolemanager = rolemanager;
            _signinmanager = signinmanager;
        }

        [HttpPost("/Register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userEmail = await _usermanager.FindByEmailAsync(registerDto.Email);

            if(userEmail != null)
            {
                return BadRequest(new { message = "The user email is already registered" });
            }

            var userName = await _usermanager.FindByNameAsync(registerDto.UserName);

            if(userName != null)
            {
                return BadRequest(new { message = "The username is already taken. Create a unique username" });
            }
            ApplicationUser newUser = new ApplicationUser
            {
                UserName = registerDto.UserName,
                Email = registerDto.Email,

            };

            IdentityResult result = await _usermanager.CreateAsync(newUser, registerDto.Password);

            if (result.Succeeded)
            {
                var defaultRole = "User";

                if(await _rolemanager.RoleExistsAsync(defaultRole))
                {
                    await _usermanager.AddToRoleAsync(newUser, defaultRole);
                } else
                {
                    Console.WriteLine("Role doesn't exist");
                }

                return Ok(new { message = "User Created successfully" });

            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }
            return BadRequest(ModelState);

        }

        [HttpPost("/Login")]

        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "the request has a problem" });
            }

            ApplicationUser? user = await _usermanager.FindByNameAsync(loginDto.UserNameOrEmail);

            if(user ==null || loginDto.UserNameOrEmail.Contains("@"))
            {
                user = await _usermanager.FindByEmailAsync(loginDto.UserNameOrEmail);
            }

            if(user == null)
            {
                return Unauthorized(new { message = "The username or password doesn't exist" });
            }

            var result = await _signinmanager.PasswordSignInAsync(user, loginDto.Password, isPersistent: false, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                return Ok(new { message="You are logged in" });
            }

            if (result.IsLockedOut)
            {
                return Unauthorized(new { message = "You are locked out due to too many attempts" });
            }

            if (result.IsNotAllowed)
            {
                return Unauthorized(new { message = "Login failed contact customer service" });
            }

            return Unauthorized(new { message = " Invalid username and password" });
        }

    
    }
}
