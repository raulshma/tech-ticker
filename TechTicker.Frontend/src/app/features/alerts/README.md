# Alert System - Frontend Implementation

This directory contains the complete Angular frontend implementation for the TechTicker Alert System.

## Overview

The alert system allows users to create, manage, and monitor price and availability alerts for products. It includes comprehensive performance monitoring and admin management capabilities.

## Features

### User Features
- **Create Alert Rules**: Set up price drop alerts, percentage drop alerts, and back-in-stock notifications
- **Manage Alerts**: View, edit, enable/disable, and delete alert rules
- **Product Search**: Autocomplete product selection when creating alerts
- **Real-time Status**: See active/inactive status and last triggered information

### Admin Features
- **Performance Monitoring**: Comprehensive dashboard showing system health and metrics
- **Alert Analytics**: View evaluation metrics, notification delivery stats, and trends
- **System Events**: Monitor system events and errors
- **Bulk Operations**: Manage multiple alert rules at once (via API)

## Components

### Core Components

#### `AlertsListComponent`
- Displays user's alert rules in a table format
- Supports toggling alert status
- Provides edit, test, and delete actions
- Shows empty state for new users

#### `AlertFormComponent`
- Comprehensive form for creating and editing alerts
- Product autocomplete with search functionality
- Conditional validation based on alert type
- Support for all alert condition types:
  - Price below threshold
  - Percentage drop from last price
  - Back in stock notifications

#### `AlertPerformanceComponent`
- Admin dashboard for monitoring alert system performance
- Real-time metrics and system health indicators
- Performance trends and analytics
- System event monitoring

### Page Components

#### `AlertCreateComponent`
- Wrapper page for creating new alerts
- Supports pre-selecting products via query parameters
- Handles navigation after successful creation

#### `AlertEditComponent`
- Wrapper page for editing existing alerts
- Loads alert data and handles updates
- Error handling for missing alerts

## Services

### `AlertsService`
Core service for alert CRUD operations:
- `getUserAlerts()`: Get user's alert rules
- `getProductAlerts(productId)`: Get alerts for specific product
- `createAlert(alertRule)`: Create new alert rule
- `updateAlert(alertRuleId, updates)`: Update existing alert
- `deleteAlert(alertRuleId)`: Delete alert rule

### `AlertPerformanceService`
Service for performance monitoring (Admin only):
- `getSystemPerformance()`: Overall system metrics
- `getSystemHealth()`: Health status and issues
- `getRealTimeMonitoring()`: Live activity data
- `getPerformanceTrends()`: Historical performance data
- `getNotificationChannelStats()`: Channel-specific metrics

### `ProductsService`
Supporting service for product operations:
- `getAllProducts()`: Get all products for autocomplete
- `searchProducts(query)`: Search products by name/brand
- `getProductById(id)`: Get specific product details

## Routing

The alerts module includes the following routes:

```typescript
const routes: Routes = [
  { path: '', component: AlertsListComponent },           // /alerts
  { path: 'create', component: AlertCreateComponent },    // /alerts/create
  { path: 'edit/:id', component: AlertEditComponent },    // /alerts/edit/:id
  { path: 'performance', component: AlertPerformanceComponent } // /alerts/performance
];
```

## API Integration

The frontend uses the NSwag-generated TypeScript client (`TechTickerApiClient`) to communicate with the backend API. The client is automatically generated from the OpenAPI specification.

### Key API Endpoints Used
- `GET /api/alerts` - Get user alerts
- `POST /api/alerts` - Create alert
- `PUT /api/alerts/{id}` - Update alert
- `DELETE /api/alerts/{id}` - Delete alert
- `GET /api/alert-performance/*` - Performance monitoring endpoints

## Usage Examples

### Creating an Alert from Product Page
```typescript
// Navigate to create alert with pre-selected product
this.router.navigate(['/alerts/create'], { 
  queryParams: { productId: 'product-guid' } 
});
```

### Monitoring Alert Performance (Admin)
```typescript
// Navigate to performance dashboard
this.router.navigate(['/alerts/performance']);
```

### Toggling Alert Status
```typescript
// The AlertsListComponent handles this automatically
toggleAlert(alert: AlertRuleDto, isActive: boolean): void {
  this.alertsService.updateAlert(alert.alertRuleId!, { isActive })
    .subscribe(/* handle response */);
}
```

## Styling and UX

The components use Angular Material for consistent UI:
- **Material Design**: Cards, tables, forms, and buttons
- **Responsive Layout**: Works on desktop and mobile
- **Loading States**: Spinners and progress indicators
- **Error Handling**: Snackbar notifications for user feedback
- **Empty States**: Helpful messages for new users

## Security

- **Role-based Access**: Users can only see their own alerts
- **Admin Features**: Performance monitoring requires admin role
- **Input Validation**: Client-side validation with server-side enforcement
- **Error Handling**: Graceful handling of API errors

## Development Notes

### Adding New Alert Conditions
1. Update the backend API to support the new condition
2. Add the condition type to the form dropdown
3. Update validation logic in `AlertFormComponent`
4. Add display logic in `AlertsListComponent`

### Extending Performance Monitoring
1. Add new metrics to the backend API
2. Update `AlertPerformanceService` to fetch new data
3. Add new visualizations to `AlertPerformanceComponent`

### Testing
- Unit tests should cover service methods and component logic
- Integration tests should verify API communication
- E2E tests should cover complete user workflows

## Dependencies

The alert system relies on:
- **Angular Material**: UI components
- **RxJS**: Reactive programming
- **Angular Router**: Navigation
- **Angular Forms**: Reactive forms
- **NSwag Generated Client**: API communication

## Future Enhancements

Potential improvements:
- **Real-time Updates**: WebSocket integration for live alerts
- **Charts and Graphs**: Visual performance analytics
- **Alert Templates**: Pre-configured alert setups
- **Bulk Import/Export**: CSV import/export functionality
- **Advanced Filtering**: Complex alert rule filtering
- **Mobile App**: Native mobile application
