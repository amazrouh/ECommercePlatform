using Shared.Contracts.DTOs;

namespace ProductService.Services;

/// <summary>
/// Product service interface.
/// </summary>
public interface IProductService
{
    Task<PagedResult<ProductSummaryDto>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductDto?> UpdateAsync(Guid productId, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<bool> UpdateStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
    Task<IEnumerable<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<CategoryDto?> GetCategoryByIdAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
}
