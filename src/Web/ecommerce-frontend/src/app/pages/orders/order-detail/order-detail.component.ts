import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { OrderService, Order } from '../../../core/services/order.service';

@Component({
  selector: 'app-order-detail',
  templateUrl: './order-detail.component.html',
  styleUrls: ['./order-detail.component.scss']
})
export class OrderDetailComponent implements OnInit {
  order: Order | null = null;
  loading = true;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private orderService: OrderService
  ) {}

  ngOnInit(): void {
    const orderId = this.route.snapshot.paramMap.get('id');
    if (orderId) {
      this.loadOrder(orderId);
    }
  }

  loadOrder(orderId: string): void {
    this.orderService.getOrder(orderId).subscribe({
      next: (order) => {
        this.order = order;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.router.navigate(['/orders']);
      }
    });
  }

  cancelOrder(): void {
    if (!this.order) return;
    const reason = prompt('Please enter a reason for cancellation:');
    if (reason) {
      this.orderService.cancelOrder(this.order.id, reason).subscribe({
        next: (order) => this.order = order,
        error: (err) => alert(err.error?.message || 'Failed to cancel order')
      });
    }
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

  canCancel(): boolean {
    return this.order?.status === 0 || this.order?.status === 1;
  }
}
