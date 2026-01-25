import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ProductService, Product } from '../../../core/services/product.service';
import { CartService } from '../../../core/services/cart.service';

@Component({
  selector: 'app-product-detail',
  templateUrl: './product-detail.component.html',
  styleUrls: ['./product-detail.component.scss']
})
export class ProductDetailComponent implements OnInit {
  product: Product | null = null;
  loading = true;
  quantity = 1;
  selectedImage: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private productService: ProductService,
    private cartService: CartService
  ) {}

  ngOnInit(): void {
    const productId = this.route.snapshot.paramMap.get('id');
    if (productId) {
      this.loadProduct(productId);
    }
  }

  loadProduct(productId: string): void {
    this.productService.getProduct(productId).subscribe({
      next: (product) => {
        this.product = product;
        this.selectedImage = product.thumbnailUrl || product.imageUrls[0];
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.router.navigate(['/products']);
      }
    });
  }

  selectImage(url: string): void {
    this.selectedImage = url;
  }

  incrementQuantity(): void {
    if (this.product && this.quantity < this.product.stockQuantity) {
      this.quantity++;
    }
  }

  decrementQuantity(): void {
    if (this.quantity > 1) {
      this.quantity--;
    }
  }

  addToCart(): void {
    if (!this.product) return;

    this.cartService.addToCart(this.product.id, this.quantity).subscribe({
      next: () => {
        alert(`${this.quantity} x ${this.product!.name} added to cart!`);
      },
      error: () => alert('Please login to add items to cart')
    });
  }
}
