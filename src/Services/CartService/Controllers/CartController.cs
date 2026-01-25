using CartService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.DTOs;

namespace CartService.Controllers;

/// <summary>
/// Shopping cart controller.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly ILogger<CartController> _logger;

    public CartController(ICartService cartService, ILogger<CartController> logger)
    {
        _cartService = cartService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's cart.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CartDto>> GetCart(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var cart = await _cartService.GetCartAsync(userId.Value, cancellationToken);
        return Ok(cart);
    }

    /// <summary>
    /// Get cart summary (item count and total).
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(CartSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CartSummaryDto>> GetCartSummary(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var summary = await _cartService.GetCartSummaryAsync(userId.Value, cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// Add item to cart.
    /// </summary>
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CartDto>> AddToCart([FromBody] AddToCartRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var cart = await _cartService.AddToCartAsync(userId.Value, request, cancellationToken);
            return Ok(cart);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update cart item quantity.
    /// </summary>
    [HttpPut("items/{itemId:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> UpdateCartItem(Guid itemId, [FromBody] UpdateCartItemRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var cart = await _cartService.UpdateCartItemAsync(userId.Value, itemId, request, cancellationToken);
        if (cart == null) return NotFound();

        return Ok(cart);
    }

    /// <summary>
    /// Remove item from cart.
    /// </summary>
    [HttpDelete("items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFromCart(Guid itemId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var success = await _cartService.RemoveFromCartAsync(userId.Value, itemId, cancellationToken);
        if (!success) return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Clear all items from cart.
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CartDto>> ClearCart(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var cart = await _cartService.ClearCartAsync(userId.Value, cancellationToken);
        return Ok(cart);
    }

    /// <summary>
    /// Apply coupon to cart.
    /// </summary>
    [HttpPost("coupon")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CartDto>> ApplyCoupon([FromBody] ApplyCouponRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var cart = await _cartService.ApplyCouponAsync(userId.Value, request, cancellationToken);
            if (cart == null) return NotFound();
            return Ok(cart);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove coupon from cart.
    /// </summary>
    [HttpDelete("coupon")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CartDto>> RemoveCoupon(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var cart = await _cartService.RemoveCouponAsync(userId.Value, cancellationToken);
        return Ok(cart);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
