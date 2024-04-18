using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AuthGate.Model;
using AuthGate.DTO;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthGate.Validators;

namespace AuthGate.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly string _jwtKey;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            ILogger<AuthController> logger
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _jwtKey = configuration["JwtKey"];
            _logger = logger;
        }

        [HttpPost("register/admin")]
        public async Task<IActionResult> RegisterAdmin([FromBody] AdminRegisterDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                _logger.LogInformation("Admin role does not exist; creating new admin role.");
                var roleResult = await _roleManager.CreateAsync(new IdentityRole("Admin"));
                if (!roleResult.Succeeded)
                {
                    _logger.LogError("Failed to create admin role. Errors: {Errors}", roleResult.Errors);
                    return BadRequest(roleResult.Errors);
                }
            }

            var adminUser = new AdminUser { UserName = model.Email, Email = model.Email };
            var userCreationResult = await _userManager.CreateAsync(adminUser, model.Password);
            if (!userCreationResult.Succeeded)
            {
                _logger.LogError("Failed to create admin user for {Email}. Errors: {Errors}", model.Email, userCreationResult.Errors);
                return BadRequest(userCreationResult.Errors);
            }

            _logger.LogInformation("Admin user {UserId} created successfully, assigning 'Admin' role.", adminUser.Id);
            var roleAssignmentResult = await _userManager.AddToRoleAsync(adminUser, "Admin");

            if (!roleAssignmentResult.Succeeded)
            {
                _logger.LogError("Failed to assign 'Admin' role to user {UserId}. Errors: {Errors}", adminUser.Id, roleAssignmentResult.Errors);
                await _userManager.DeleteAsync(adminUser);
                return BadRequest(roleAssignmentResult.Errors);
            }

            _logger.LogInformation("Admin user {UserId} successfully registered and signed in.", adminUser.Id);
            await _signInManager.SignInAsync(adminUser, isPersistent: false);
            return Ok(new { UserId = adminUser.Id });
        }



        [HttpPost("register/rider")]
        public async Task<IActionResult> RegisterRider([FromBody] RiderRegisterDto model)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!await _roleManager.RoleExistsAsync("Rider"))
            {
                _logger.LogInformation("Rider role does not exist; creating new rider role.");
                await _roleManager.CreateAsync(new IdentityRole("Rider"));
            }

            var (isValid, parsedCNHType) = CnhValidator.ParseCNHType(model.TipoCNH);
            if (!isValid)
            {
                ModelState.AddModelError("TipoCNH", "Invalid CNH Type");
                return BadRequest(ModelState);
            }

            var riderUser = new RiderUser
            {
                UserName = model.Email,
                Email = model.Email,
                CNPJ = model.CNPJ,
                DataNascimento = model.DataNascimento,
                NumeroCNH = model.NumeroCNH,
                TipoCNH = parsedCNHType,
                ImagemCNH = model.ImagemCNH
            };

            var result = await _userManager.CreateAsync(riderUser, model.Password);
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to create rider user for {Email}. Errors: {Errors}", model.Email, result.Errors);
                return BadRequest(result.Errors);
            }

            _logger.LogInformation("Rider user {UserId} created successfully, assigning 'Rider' role.", riderUser.Id);
            var roleAssignmentResult = await _userManager.AddToRoleAsync(riderUser, "Rider");

            if (!roleAssignmentResult.Succeeded)
            {
                _logger.LogError("Failed to assign 'Rider' role to user {UserId}. Errors: {Errors}", riderUser.Id, roleAssignmentResult.Errors);
                await _userManager.DeleteAsync(riderUser);
                return BadRequest(roleAssignmentResult.Errors);
            }

            _logger.LogInformation("Rider user {UserId} successfully registered and signed in.", riderUser.Id);
            await _signInManager.SignInAsync(riderUser, isPersistent: false);
            return Ok(new { UserId = riderUser.Id });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Login failed for user {Email}: Invalid model state", model.Email);
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed: no user found with email {Email}", model.Email);
                return Unauthorized();
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, true, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Login failed for user {Email}: {Reason}", model.Email, result.IsLockedOut ? "Account locked out" : "Invalid credentials");
                return Unauthorized();
            }

            _logger.LogInformation("User {Email} successfully authenticated, preparing to generate JWT token.", model.Email);

            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds);

            var tokenHandler = new JwtSecurityTokenHandler();
            var stringToken = tokenHandler.WriteToken(token);

            _logger.LogInformation("JWT token generated for user {Email}.", model.Email);

            return Ok(new { token = stringToken });
        }


        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutDto model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Email))
            {
                _logger.LogWarning("Logout attempt failed: No email provided.");
                return BadRequest("Email must be provided for logout.");
            }

            _logger.LogInformation("User {Email} initiating logout", model.Email);
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User {Email} logged out successfully", model.Email);
            return Ok();
        }
    }
}
