using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Services;
using Shared.Contracts.DTOs;

namespace ProductService.Controllers;

/// <summary>
/// Categories controller.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(IProductService productService, ILogger<CategoriesController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    /// <summary>
    /// Get all categories.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories(CancellationToken cancellationToken)
    {
        var categories = await _productService.GetCategoriesAsync(cancellationToken);
        return Ok(categories);
    }

    /// <summary>
    /// Get category by ID.
    /// </summary>
    [HttpGet("{categoryId:guid}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> GetById(Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await _productService.GetCategoryByIdAsync(categoryId, cancellationToken);
        if (category == null) return NotFound();

        return Ok(category);
    }

    /// <summary>
    /// Create a new category (Admin only).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await _productService.CreateCategoryAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { categoryId = category.Id }, category);
    }
}
