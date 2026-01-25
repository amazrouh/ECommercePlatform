using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Services;
using Shared.Contracts.DTOs;

namespace ProductService.Controllers;

/// <summary>
/// Products controller.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    /// <summary>
    /// Search and filter products.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductSummaryDto>>> Search([FromQuery] ProductSearchRequest request, CancellationToken cancellationToken)
    {
        var result = await _productService.SearchAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get product by ID.
    /// </summary>
    [HttpGet("{productId:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(Guid productId, CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(productId, cancellationToken);
        if (product == null) return NotFound();

        return Ok(product);
    }

    /// <summary>
    /// Get product by SKU.
    /// </summary>
    [HttpGet("sku/{sku}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetBySku(string sku, CancellationToken cancellationToken)
    {
        var product = await _productService.GetBySkuAsync(sku, cancellationToken);
        if (product == null) return NotFound();

        return Ok(product);
    }

    /// <summary>
    /// Create a new product (Admin only).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await _productService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { productId = product.Id }, product);
    }

    /// <summary>
    /// Update a product (Admin only).
    /// </summary>
    [HttpPut("{productId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Update(Guid productId, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await _productService.UpdateAsync(productId, request, cancellationToken);
        if (product == null) return NotFound();

        return Ok(product);
    }

    /// <summary>
    /// Delete a product (Admin only).
    /// </summary>
    [HttpDelete("{productId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid productId, CancellationToken cancellationToken)
    {
        var success = await _productService.DeleteAsync(productId, cancellationToken);
        if (!success) return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Update product stock (Admin only).
    /// </summary>
    [HttpPatch("{productId:guid}/stock")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStock(Guid productId, [FromBody] UpdateStockRequest request, CancellationToken cancellationToken)
    {
        var success = await _productService.UpdateStockAsync(productId, request.Quantity, cancellationToken);
        if (!success) return NotFound();

        return Ok(new { message = "Stock updated successfully" });
    }
}

/// <summary>
/// Update stock request.
/// </summary>
public record UpdateStockRequest
{
    public int Quantity { get; init; }
}
