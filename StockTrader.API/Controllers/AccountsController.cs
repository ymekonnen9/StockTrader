using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StockTrader.Application.DTOs;
using StockTrader.Application.Services; // Assuming ITokenService is here
using StockTrader.Domain.Entities;

namespace StockTrader.API.Controllers
{
    [Route("api/[controller]")] // Base route: /api/accounts
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager; // Corrected casing
        private readonly RoleManager<IdentityRole> _roleManager;   // Corrected casing
        private readonly SignInManager<ApplicationUser> _signInManager; // Corrected casing
        private readonly ITokenService _tokenService;           // Corrected casing
        private readonly ILogger<AccountsController> _logger; // Added logger for consistency

        public AccountsController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            ILogger<AccountsController> logger) // Added logger
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _logger = logger;
        }

        // Route will now be: POST /api/accounts/register
        [HttpPost("register")] 
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingUserByEmail = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUserByEmail != null)
            {
                _logger.LogWarning("Registration attempt with existing email: {Email}", registerDto.Email);
                return BadRequest(new { Message = "Email already in use. Please try a different email or log in." });
            }

            // Corrected property name from DTO: registerDto.Username (not UserName)
            var existingUserByUsername = await _userManager.FindByNameAsync(registerDto.UserName);
            if (existingUserByUsername != null)
            {
                _logger.LogWarning("Registration attempt with existing username: {Username}", registerDto.UserName);
                return BadRequest(new { Message = "Username already taken. Please choose a different username." });
            }

            ApplicationUser newUser = new ApplicationUser
            {
                UserName = registerDto.UserName, // Corrected DTO property
                Email = registerDto.Email,
                // CashBalance is set by ApplicationUser constructor
            };

            IdentityResult result = await _userManager.CreateAsync(newUser, registerDto.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Username} registered successfully.", newUser.UserName);
                var defaultRole = "User"; // Ensure this role is seeded by your DataSeeder

                if (await _roleManager.RoleExistsAsync(defaultRole))
                {
                    await _userManager.AddToRoleAsync(newUser, defaultRole);
                    _logger.LogInformation("Assigned role '{Role}' to user {Username}.", defaultRole, newUser.UserName);
                }
                else
                {
                    _logger.LogWarning("Default role '{Role}' not found for user {Username}. Seeding might be required.", defaultRole, newUser.UserName);
                }
                return Ok(new { Message = "User created successfully. You can now log in." }); // Changed message slightly for clarity
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
                _logger.LogWarning("Registration error for {Username}: {ErrorCode} - {ErrorDescription}", registerDto.UserName, error.Code, error.Description);
            }
            return BadRequest(ModelState);
        }

        // Route will now be: POST /api/accounts/login
        [HttpPost("login")] 
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Login attempt for {UsernameOrEmail}", loginDto.UserNameOrEmail);

            ApplicationUser? user = await _userManager.FindByNameAsync(loginDto.UserNameOrEmail);
            if (user == null && loginDto.UserNameOrEmail.Contains("@"))
            {
                user = await _userManager.FindByEmailAsync(loginDto.UserNameOrEmail);
            }

            if (user == null)
            {
                _logger.LogWarning("Login failed for {UsernameOrEmail}: User not found.", loginDto.UserNameOrEmail);
                return Unauthorized(new { Message = "Invalid username or password." }); // Generic message
            }

            var result = await _signInManager.PasswordSignInAsync(user, loginDto.Password, isPersistent: false, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Username} logged in successfully.", user.UserName);
                var accessToken = await _tokenService.GenerateTokenAsync(user);
                return Ok(new
                {
                    Message = "Login successful. Welcome!",
                    Token = accessToken,
                    Username = user.UserName, // Ensure UserName is not null
                    Email = user.Email    // Ensure Email is not null
                });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User {Username} account locked out.", user.UserName);
                return Unauthorized(new { Message = "Account locked out due to multiple failed login attempts. Please try again later." });
            }

            if (result.IsNotAllowed)
            {
                _logger.LogWarning("Login not allowed for {Username}. Possible reasons: email not confirmed (if enabled).", user.UserName);
                return Unauthorized(new { Message = "Login not allowed. Please confirm your email or contact support." });
            }

            _logger.LogWarning("Invalid password attempt for user {Username}.", user.UserName);
            return Unauthorized(new { Message = "Invalid username or password." });
        }
    }
}
