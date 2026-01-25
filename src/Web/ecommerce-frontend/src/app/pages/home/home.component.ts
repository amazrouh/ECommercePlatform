import { Component, OnInit } from '@angular/core';
import { ProductService, ProductSummary, Category } from '../../core/services/product.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit {
  featuredProducts: ProductSummary[] = [];
  categories: Category[] = [];
  loading = true;

  constructor(private productService: ProductService) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.productService.searchProducts({ pageSize: 8, sortBy: 'rating', sortDescending: true })
      .subscribe({
        next: (result) => {
          this.featuredProducts = result.items;
          this.loading = false;
        },
        error: () => this.loading = false
      });

    this.productService.getCategories().subscribe({
      next: (categories) => this.categories = categories
    });
  }
}
