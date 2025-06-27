import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  TechTickerApiClient,
  UpdateUserNotificationPreferencesDto,
  TestDiscordWebhookDto,
  UserNotificationPreferencesDtoApiResponse,
  NotificationProductSelectionDtoIEnumerableApiResponse,
  NotificationPreferencesSummaryDtoApiResponse,
  ApiResponse
} from '../../../shared/api/api-client';

@Injectable({
  providedIn: 'root'
})
export class NotificationSettingsService {

  constructor(private apiClient: TechTickerApiClient) {}

  /**
   * Get current user's notification preferences
   */
  getNotificationPreferences(): Observable<UserNotificationPreferencesDtoApiResponse> {
    return this.apiClient.getNotificationPreferences();
  }

  /**
   * Update current user's notification preferences
   */
  updateNotificationPreferences(updateDto: UpdateUserNotificationPreferencesDto): Observable<UserNotificationPreferencesDtoApiResponse> {
    return this.apiClient.updateNotificationPreferences(updateDto);
  }

  /**
   * Get available products for notification selection
   */
  getAvailableProductsForNotification(): Observable<NotificationProductSelectionDtoIEnumerableApiResponse> {
    return this.apiClient.getAvailableProductsForNotification();
  }

  /**
   * Test Discord webhook configuration
   */
  testDiscordWebhook(testDto: TestDiscordWebhookDto): Observable<ApiResponse> {
    return this.apiClient.testDiscordWebhook(testDto);
  }

  /**
   * Get notification preferences summary
   */
  getNotificationPreferencesSummary(): Observable<NotificationPreferencesSummaryDtoApiResponse> {
    return this.apiClient.getNotificationPreferencesSummary();
  }
}
