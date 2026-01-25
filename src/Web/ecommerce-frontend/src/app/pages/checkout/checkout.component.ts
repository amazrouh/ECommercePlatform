import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CartService, Cart } from '../../core/services/cart.service';
import { OrderService, CreateOrderRequest } from '../../core/services/order.service';

@Component({
  selector: 'app-checkout',
  templateUrl: './checkout.component.html',
  styleUrls: ['./checkout.component.scss']
})
export class CheckoutComponent implements OnInit {
  cart: Cart | null = null;
  checkoutForm: FormGroup;
  loading = true;
  submitting = false;
  step = 1; // 1: Shipping, 2: Payment, 3: Review

  constructor(
    private fb: FormBuilder,
    private cartService: CartService,
    private orderService: OrderService,
    private router: Router
  ) {
    this.checkoutForm = this.fb.group({
      shippingAddress: this.fb.group({
        street: ['', [Validators.required]],
        city: ['', [Validators.required]],
        state: ['', [Validators.required]],
        postalCode: ['', [Validators.required]],
        country: ['USA', [Validators.required]]
      }),
      billingSameAsShipping: [true],
      billingAddress: this.fb.group({
        street: [''],
        city: [''],
        state: [''],
        postalCode: [''],
        country: ['USA']
      }),
      payment: this.fb.group({
        paymentMethod: ['CreditCard', [Validators.required]],
        cardNumber: ['4111111111111111', [Validators.required]],
        cardHolderName: ['John Doe', [Validators.required]],
        expiryMonth: ['12', [Validators.required]],
        expiryYear: ['2025', [Validators.required]],
        cvv: ['123', [Validators.required]]
      }),
      notes: ['']
    });
  }

  ngOnInit(): void {
    this.cartService.getCart().subscribe({
      next: (cart) => {
        this.cart = cart;
        this.loading = false;
        if (cart.items.length === 0) {
          this.router.navigate(['/cart']);
        }
      },
      error: () => this.router.navigate(['/cart'])
    });
  }

  nextStep(): void {
    if (this.step < 3) this.step++;
  }

  prevStep(): void {
    if (this.step > 1) this.step--;
  }

  submitOrder(): void {
    if (this.checkoutForm.invalid) return;

    this.submitting = true;
    const formValue = this.checkoutForm.value;

    const request: CreateOrderRequest = {
      shippingAddress: formValue.shippingAddress,
      billingAddress: formValue.billingSameAsShipping ? undefined : formValue.billingAddress,
      billingSameAsShipping: formValue.billingSameAsShipping,
      payment: formValue.payment,
      notes: formValue.notes,
      couponCode: this.cart?.couponCode || undefined
    };

    this.orderService.createOrder(request).subscribe({
      next: (order) => {
        this.cartService.clearCart().subscribe();
        alert(`Order placed successfully! Order number: ${order.orderNumber}`);
        this.router.navigate(['/orders', order.id]);
      },
      error: (err) => {
        this.submitting = false;
        alert(err.error?.message || 'Failed to place order');
      }
    });
  }
}
