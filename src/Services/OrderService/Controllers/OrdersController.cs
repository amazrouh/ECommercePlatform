using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Services;
using Shared.Contracts.DTOs;

namespace OrderService.Controllers;

/// <summary>
/// Orders controller.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new order (checkout).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "customer@example.com";
        var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "Customer";

        try
        {
            var order = await _orderService.CreateOrderAsync(userId.Value, userEmail, userName, request, cancellationToken);
            return CreatedAtAction(nameof(GetOrder), new { orderId = order.Id }, order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get order by ID.
    /// </summary>
    [HttpGet("{orderId:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrder(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId, cancellationToken);
        if (order == null) return NotFound();

        // Verify ownership (unless admin)
        var userId = GetCurrentUserId();
        if (userId != order.UserId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        return Ok(order);
    }

    /// <summary>
    /// Get order by order number.
    /// </summary>
    [HttpGet("number/{orderNumber}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrderByNumber(string orderNumber, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetOrderByNumberAsync(orderNumber, cancellationToken);
        if (order == null) return NotFound();

        var userId = GetCurrentUserId();
        if (userId != order.UserId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        return Ok(order);
    }

    /// <summary>
    /// Get current user's orders.
    /// </summary>
    [HttpGet("my-orders")]
    [ProducesResponseType(typeof(PagedResult<OrderSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrderSummaryDto>>> GetMyOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var orders = await _orderService.GetUserOrdersAsync(userId.Value, page, pageSize, cancellationToken);
        return Ok(orders);
    }

    /// <summary>
    /// Process order (Admin only).
    /// </summary>
    [HttpPost("{orderId:guid}/process")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> ProcessOrder(Guid orderId, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orderService.ProcessOrderAsync(orderId, cancellationToken);
            if (order == null) return NotFound();
            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Ship order (Admin only).
    /// </summary>
    [HttpPost("{orderId:guid}/ship")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> ShipOrder(Guid orderId, [FromBody] ShipOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orderService.ShipOrderAsync(orderId, request.Carrier, request.TrackingNumber, request.TrackingUrl, cancellationToken);
            if (order == null) return NotFound();
            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Mark order as delivered (Admin only).
    /// </summary>
    [HttpPost("{orderId:guid}/deliver")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> DeliverOrder(Guid orderId, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orderService.DeliverOrderAsync(orderId, cancellationToken);
            if (order == null) return NotFound();
            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cancel order.
    /// </summary>
    [HttpPost("{orderId:guid}/cancel")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> CancelOrder(Guid orderId, [FromBody] CancelOrderRequest request, CancellationToken cancellationToken)
    {
        var existingOrder = await _orderService.GetOrderByIdAsync(orderId, cancellationToken);
        if (existingOrder == null) return NotFound();

        var userId = GetCurrentUserId();
        if (userId != existingOrder.UserId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        try
        {
            var order = await _orderService.CancelOrderAsync(orderId, request.Reason, cancellationToken);
            if (order == null) return NotFound();
            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

/// <summary>
/// Ship order request.
/// </summary>
public record ShipOrderRequest
{
    public string Carrier { get; init; } = string.Empty;
    public string TrackingNumber { get; init; } = string.Empty;
    public string? TrackingUrl { get; init; }
}
