import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Product {
  id: string;
  name: string;
  description: string;
  sku: string;
  price: number;
  salePrice?: number;
  stockQuantity: number;
  isInStock: boolean;
  categoryId: string;
  categoryName: string;
  imageUrls: string[];
  thumbnailUrl?: string;
  averageRating: number;
  reviewCount: number;
}

export interface ProductSummary {
  id: string;
  name: string;
  price: number;
  salePrice?: number;
  thumbnailUrl?: string;
  averageRating: number;
  reviewCount: number;
  isInStock: boolean;
}

export interface Category {
  id: string;
  name: string;
  description?: string;
  imageUrl?: string;
  productCount: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface ProductSearchParams {
  searchTerm?: string;
  categoryId?: string;
  minPrice?: number;
  maxPrice?: number;
  inStockOnly?: boolean;
  sortBy?: string;
  sortDescending?: boolean;
  page?: number;
  pageSize?: number;
}

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  constructor(private http: HttpClient) {}

  searchProducts(params: ProductSearchParams): Observable<PagedResult<ProductSummary>> {
    let httpParams = new HttpParams();

    if (params.searchTerm) httpParams = httpParams.set('searchTerm', params.searchTerm);
    if (params.categoryId) httpParams = httpParams.set('categoryId', params.categoryId);
    if (params.minPrice !== undefined) httpParams = httpParams.set('minPrice', params.minPrice.toString());
    if (params.maxPrice !== undefined) httpParams = httpParams.set('maxPrice', params.maxPrice.toString());
    if (params.inStockOnly !== undefined) httpParams = httpParams.set('inStockOnly', params.inStockOnly.toString());
    if (params.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);
    if (params.sortDescending !== undefined) httpParams = httpParams.set('sortDescending', params.sortDescending.toString());
    if (params.page !== undefined) httpParams = httpParams.set('page', params.page.toString());
    if (params.pageSize !== undefined) httpParams = httpParams.set('pageSize', params.pageSize.toString());

    return this.http.get<PagedResult<ProductSummary>>(`${environment.apiUrl}/products`, { params: httpParams });
  }

  getProduct(productId: string): Observable<Product> {
    return this.http.get<Product>(`${environment.apiUrl}/products/${productId}`);
  }

  getCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${environment.apiUrl}/categories`);
  }

  getCategory(categoryId: string): Observable<Category> {
    return this.http.get<Category>(`${environment.apiUrl}/categories/${categoryId}`);
  }
}
