import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ProductService, ProductSummary, Category, PagedResult, ProductSearchParams } from '../../../core/services/product.service';
import { CartService } from '../../../core/services/cart.service';

@Component({
  selector: 'app-product-list',
  templateUrl: './product-list.component.html',
  styleUrls: ['./product-list.component.scss']
})
export class ProductListComponent implements OnInit {
  products: ProductSummary[] = [];
  categories: Category[] = [];
  loading = true;

  // Pagination
  currentPage = 1;
  pageSize = 12;
  totalCount = 0;
  totalPages = 0;

  // Filters
  searchTerm = '';
  selectedCategoryId = '';
  sortBy = 'name';
  sortDescending = false;

  constructor(
    private productService: ProductService,
    private cartService: CartService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadCategories();

    this.route.queryParams.subscribe(params => {
      this.searchTerm = params['searchTerm'] || '';
      this.selectedCategoryId = params['categoryId'] || '';
      this.currentPage = parseInt(params['page']) || 1;
      this.loadProducts();
    });
  }

  loadCategories(): void {
    this.productService.getCategories().subscribe({
      next: (categories) => this.categories = categories
    });
  }

  loadProducts(): void {
    this.loading = true;

    const params: ProductSearchParams = {
      searchTerm: this.searchTerm || undefined,
      categoryId: this.selectedCategoryId || undefined,
      sortBy: this.sortBy,
      sortDescending: this.sortDescending,
      page: this.currentPage,
      pageSize: this.pageSize
    };

    this.productService.searchProducts(params).subscribe({
      next: (result: PagedResult<ProductSummary>) => {
        this.products = result.items;
        this.totalCount = result.totalCount;
        this.totalPages = result.totalPages;
        this.loading = false;
      },
      error: () => this.loading = false
    });
  }

  onSearch(): void {
    this.currentPage = 1;
    this.updateUrl();
    this.loadProducts();
  }

  onCategoryChange(): void {
    this.currentPage = 1;
    this.updateUrl();
    this.loadProducts();
  }

  onSortChange(): void {
    this.loadProducts();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.updateUrl();
    this.loadProducts();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  private updateUrl(): void {
    const queryParams: any = {};
    if (this.searchTerm) queryParams.searchTerm = this.searchTerm;
    if (this.selectedCategoryId) queryParams.categoryId = this.selectedCategoryId;
    if (this.currentPage > 1) queryParams.page = this.currentPage;

    this.router.navigate([], { queryParams });
  }

  addToCart(product: ProductSummary): void {
    this.cartService.addToCart(product.id, 1).subscribe({
      next: () => alert(`${product.name} added to cart!`),
      error: () => alert('Please login to add items to cart')
    });
  }
}
