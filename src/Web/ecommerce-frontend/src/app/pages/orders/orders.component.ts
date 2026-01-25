import { Component, OnInit } from '@angular/core';
import { OrderService, OrderSummary, PagedResult } from '../../core/services/order.service';

@Component({
  selector: 'app-orders',
  templateUrl: './orders.component.html',
  styleUrls: ['./orders.component.scss']
})
export class OrdersComponent implements OnInit {
  orders: OrderSummary[] = [];
  loading = true;
  currentPage = 1;
  totalPages = 1;

  constructor(private orderService: OrderService) {}

  ngOnInit(): void {
    this.loadOrders();
  }

  loadOrders(): void {
    this.orderService.getMyOrders(this.currentPage).subscribe({
      next: (result: PagedResult<OrderSummary>) => {
        this.orders = result.items;
        this.totalPages = result.totalPages;
        this.loading = false;
      },
      error: () => this.loading = false
    });
  }

  getStatusClass(status: number): string {
    switch (status) {
      case 0: return 'pending';
      case 1: case 2: return 'processing';
      case 3: return 'shipped';
      case 4: case 5: return 'completed';
      case 6: case 7: return 'cancelled';
      default: return '';
    }
  }
}
