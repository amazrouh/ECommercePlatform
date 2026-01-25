import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService, User } from '../../../core/services/auth.service';
import { CartService, CartSummary } from '../../../core/services/cart.service';

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss']
})
export class HeaderComponent implements OnInit {
  currentUser: User | null = null;
  cartSummary: CartSummary = { itemCount: 0, total: 0 };
  isMenuOpen = false;

  constructor(
    private authService: AuthService,
    private cartService: CartService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
      if (user) {
        this.cartService.getCartSummary().subscribe();
      }
    });

    this.cartService.cartSummary$.subscribe(summary => {
      this.cartSummary = summary;
    });
  }

  logout(): void {
    this.authService.logout();
    this.cartService.resetCart();
    this.router.navigate(['/']);
  }

  toggleMenu(): void {
    this.isMenuOpen = !this.isMenuOpen;
  }
}
