using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class AccountApiController : ControllerBase
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;

    public AccountApiController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // POST /api/accountapi/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { error = "Username and password required" });

        var user = new IdentityUser { UserName = dto.Username };
        var result = await _userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await _signInManager.SignInAsync(user, isPersistent: false);
        return Ok(new { message = "Registration successful" });
    }

    // POST /api/accountapi/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _signInManager.PasswordSignInAsync(
            dto.Username, dto.Password, isPersistent: false, lockoutOnFailure: false);

        if (!result.Succeeded)
            return Unauthorized(new { error = "Invalid login attempt" });

        return Ok(new { message = "Login successful" });
    }

    // POST /api/accountapi/logout
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { message = "Logged out" });
    }
}

public class RegisterDto
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class LoginDto
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}
