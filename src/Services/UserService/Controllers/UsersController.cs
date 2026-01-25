using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.DTOs;
using UserService.Services;

namespace UserService.Controllers;

/// <summary>
/// User management controller.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user profile.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var user = await _userService.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null) return NotFound();

        return Ok(user);
    }

    /// <summary>
    /// Update current user profile.
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var user = await _userService.UpdateProfileAsync(userId.Value, request, cancellationToken);
        if (user == null) return NotFound();

        return Ok(user);
    }

    /// <summary>
    /// Get current user addresses.
    /// </summary>
    [HttpGet("me/addresses")]
    [ProducesResponseType(typeof(IEnumerable<AddressDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AddressDto>>> GetAddresses(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var addresses = await _userService.GetAddressesAsync(userId.Value, cancellationToken);
        return Ok(addresses);
    }

    /// <summary>
    /// Add a new address.
    /// </summary>
    [HttpPost("me/addresses")]
    [ProducesResponseType(typeof(AddressDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AddressDto>> AddAddress([FromBody] AddressRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var address = await _userService.AddAddressAsync(userId.Value, request, cancellationToken);
        if (address == null) return NotFound();

        return CreatedAtAction(nameof(GetAddresses), address);
    }

    /// <summary>
    /// Update an address.
    /// </summary>
    [HttpPut("me/addresses/{addressId:guid}")]
    [ProducesResponseType(typeof(AddressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AddressDto>> UpdateAddress(Guid addressId, [FromBody] AddressRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var address = await _userService.UpdateAddressAsync(userId.Value, addressId, request, cancellationToken);
        if (address == null) return NotFound();

        return Ok(address);
    }

    /// <summary>
    /// Delete an address.
    /// </summary>
    [HttpDelete("me/addresses/{addressId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAddress(Guid addressId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var success = await _userService.DeleteAddressAsync(userId.Value, addressId, cancellationToken);
        if (!success) return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Get user by ID (admin only).
    /// </summary>
    [HttpGet("{userId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUser(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(userId, cancellationToken);
        if (user == null) return NotFound();

        return Ok(user);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
