import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Address {
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  isDefault?: boolean;
  addressType?: string;
}

export interface OrderItem {
  id: string;
  productId: string;
  productName: string;
  productThumbnail?: string;
  sku: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface PaymentInfo {
  paymentMethod: string;
  transactionId?: string;
  status: number;
  amount: number;
  paidAt?: string;
}

export interface ShippingInfo {
  carrier: string;
  trackingNumber?: string;
  trackingUrl?: string;
  shippedAt?: string;
  deliveredAt?: string;
  estimatedDelivery?: string;
}

export interface Order {
  id: string;
  orderNumber: string;
  userId: string;
  userEmail: string;
  userName: string;
  status: number;
  statusDisplay: string;
  items: OrderItem[];
  shippingAddress: Address;
  billingAddress?: Address;
  subtotal: number;
  tax: number;
  shippingCost: number;
  discount: number;
  total: number;
  couponCode?: string;
  payment?: PaymentInfo;
  shipping?: ShippingInfo;
  notes?: string;
  createdAt: string;
  completedAt?: string;
}

export interface OrderSummary {
  id: string;
  orderNumber: string;
  status: number;
  statusDisplay: string;
  itemCount: number;
  total: number;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface CreateOrderRequest {
  shippingAddress: Address;
  billingAddress?: Address;
  billingSameAsShipping: boolean;
  payment: {
    paymentMethod: string;
    cardNumber?: string;
    cardHolderName?: string;
    expiryMonth?: string;
    expiryYear?: string;
    cvv?: string;
  };
  notes?: string;
  couponCode?: string;
}

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  constructor(private http: HttpClient) {}

  createOrder(request: CreateOrderRequest): Observable<Order> {
    return this.http.post<Order>(`${environment.apiUrl}/orders`, request);
  }

  getOrder(orderId: string): Observable<Order> {
    return this.http.get<Order>(`${environment.apiUrl}/orders/${orderId}`);
  }

  getOrderByNumber(orderNumber: string): Observable<Order> {
    return this.http.get<Order>(`${environment.apiUrl}/orders/number/${orderNumber}`);
  }

  getMyOrders(page: number = 1, pageSize: number = 10): Observable<PagedResult<OrderSummary>> {
    return this.http.get<PagedResult<OrderSummary>>(
      `${environment.apiUrl}/orders/my-orders?page=${page}&pageSize=${pageSize}`
    );
  }

  cancelOrder(orderId: string, reason: string): Observable<Order> {
    return this.http.post<Order>(`${environment.apiUrl}/orders/${orderId}/cancel`, { reason });
  }
}
