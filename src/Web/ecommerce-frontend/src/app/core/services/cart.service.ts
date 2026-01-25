import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CartItem {
  id: string;
  productId: string;
  productName: string;
  productThumbnail?: string;
  sku: string;
  unitPrice: number;
  salePrice?: number;
  effectivePrice: number;
  quantity: number;
  lineTotal: number;
  availableStock: number;
  isAvailable: boolean;
}

export interface Cart {
  id: string;
  userId: string;
  items: CartItem[];
  subtotal: number;
  tax: number;
  shippingCost: number;
  discount: number;
  total: number;
  itemCount: number;
  couponCode?: string;
}

export interface CartSummary {
  itemCount: number;
  total: number;
}

@Injectable({
  providedIn: 'root'
})
export class CartService {
  private cartSubject = new BehaviorSubject<Cart | null>(null);
  public cart$ = this.cartSubject.asObservable();

  private cartSummarySubject = new BehaviorSubject<CartSummary>({ itemCount: 0, total: 0 });
  public cartSummary$ = this.cartSummarySubject.asObservable();

  constructor(private http: HttpClient) {}

  getCart(): Observable<Cart> {
    return this.http.get<Cart>(`${environment.apiUrl}/cart`)
      .pipe(tap(cart => {
        this.cartSubject.next(cart);
        this.cartSummarySubject.next({ itemCount: cart.itemCount, total: cart.total });
      }));
  }

  getCartSummary(): Observable<CartSummary> {
    return this.http.get<CartSummary>(`${environment.apiUrl}/cart/summary`)
      .pipe(tap(summary => this.cartSummarySubject.next(summary)));
  }

  addToCart(productId: string, quantity: number = 1): Observable<Cart> {
    return this.http.post<Cart>(`${environment.apiUrl}/cart/items`, { productId, quantity })
      .pipe(tap(cart => {
        this.cartSubject.next(cart);
        this.cartSummarySubject.next({ itemCount: cart.itemCount, total: cart.total });
      }));
  }

  updateCartItem(itemId: string, quantity: number): Observable<Cart> {
    return this.http.put<Cart>(`${environment.apiUrl}/cart/items/${itemId}`, { quantity })
      .pipe(tap(cart => {
        this.cartSubject.next(cart);
        this.cartSummarySubject.next({ itemCount: cart.itemCount, total: cart.total });
      }));
  }

  removeFromCart(itemId: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/cart/items/${itemId}`)
      .pipe(tap(() => this.getCart().subscribe()));
  }

  clearCart(): Observable<Cart> {
    return this.http.delete<Cart>(`${environment.apiUrl}/cart`)
      .pipe(tap(cart => {
        this.cartSubject.next(cart);
        this.cartSummarySubject.next({ itemCount: 0, total: 0 });
      }));
  }

  applyCoupon(couponCode: string): Observable<Cart> {
    return this.http.post<Cart>(`${environment.apiUrl}/cart/coupon`, { couponCode })
      .pipe(tap(cart => this.cartSubject.next(cart)));
  }

  removeCoupon(): Observable<Cart> {
    return this.http.delete<Cart>(`${environment.apiUrl}/cart/coupon`)
      .pipe(tap(cart => this.cartSubject.next(cart)));
  }

  resetCart(): void {
    this.cartSubject.next(null);
    this.cartSummarySubject.next({ itemCount: 0, total: 0 });
  }
}
