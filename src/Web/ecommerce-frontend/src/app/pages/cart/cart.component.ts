import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CartService, Cart, CartItem } from '../../core/services/cart.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-cart',
  templateUrl: './cart.component.html',
  styleUrls: ['./cart.component.scss']
})
export class CartComponent implements OnInit {
  cart: Cart | null = null;
  loading = true;
  couponCode = '';
  applyingCoupon = false;

  constructor(
    private cartService: CartService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    if (!this.authService.isLoggedIn) {
      this.loading = false;
      return;
    }
    this.loadCart();
  }

  loadCart(): void {
    this.cartService.getCart().subscribe({
      next: (cart) => {
        this.cart = cart;
        this.loading = false;
      },
      error: () => this.loading = false
    });
  }

  updateQuantity(item: CartItem, newQuantity: number): void {
    if (newQuantity < 1) {
      this.removeItem(item);
      return;
    }

    this.cartService.updateCartItem(item.id, newQuantity).subscribe();
  }

  removeItem(item: CartItem): void {
    if (confirm(`Remove ${item.productName} from cart?`)) {
      this.cartService.removeFromCart(item.id).subscribe();
    }
  }

  clearCart(): void {
    if (confirm('Clear all items from cart?')) {
      this.cartService.clearCart().subscribe();
    }
  }

  applyCoupon(): void {
    if (!this.couponCode.trim()) return;

    this.applyingCoupon = true;
    this.cartService.applyCoupon(this.couponCode).subscribe({
      next: () => {
        this.applyingCoupon = false;
        alert('Coupon applied successfully!');
      },
      error: (err) => {
        this.applyingCoupon = false;
        alert(err.error?.message || 'Invalid coupon code');
      }
    });
  }

  removeCoupon(): void {
    this.cartService.removeCoupon().subscribe(() => {
      this.couponCode = '';
    });
  }

  checkout(): void {
    this.router.navigate(['/checkout']);
  }
}
