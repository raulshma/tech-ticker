import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { 
  UserNotificationPreferencesDto, 
  UpdateUserNotificationPreferencesDto,
  NotificationProductSelectionDto,
  TestDiscordWebhookDto,
  NotificationPreferencesSummaryDto
} from '../../../../shared/api/api-client';
import { NotificationSettingsService } from '../../services/notification-settings.service';

@Component({
  selector: 'app-notification-settings',
  templateUrl: './notification-settings.component.html',
  styleUrls: ['./notification-settings.component.scss'],
  standalone: false
})
export class NotificationSettingsComponent implements OnInit {
  settingsForm: FormGroup;
  isLoading = false;
  isSaving = false;
  isTestingWebhook = false;
  availableProducts: NotificationProductSelectionDto[] = [];
  summary: NotificationPreferencesSummaryDto | null = null;

  constructor(
    private formBuilder: FormBuilder,
    private notificationSettingsService: NotificationSettingsService,
    private snackBar: MatSnackBar,
    private router: Router
  ) {
    this.settingsForm = this.formBuilder.group({
      isDiscordNotificationEnabled: [false],
      discordWebhookUrl: ['', [Validators.pattern(/^https:\/\/discord\.com\/api\/webhooks\/.+/)]],
      customBotName: ['', [Validators.maxLength(100)]],
      customAvatarUrl: ['', [Validators.pattern(/^https?:\/\/.+/)]],
      notificationProductIds: [[]]
    });
  }

  ngOnInit(): void {
    this.loadNotificationSettings();
    this.loadAvailableProducts();
    this.loadSummary();

    // Add conditional validation for webhook URL
    this.settingsForm.get('isDiscordNotificationEnabled')?.valueChanges.subscribe(enabled => {
      const webhookControl = this.settingsForm.get('discordWebhookUrl');
      if (enabled) {
        webhookControl?.setValidators([
          Validators.required,
          Validators.pattern(/^https:\/\/discord\.com\/api\/webhooks\/.+/)
        ]);
      } else {
        webhookControl?.clearValidators();
      }
      webhookControl?.updateValueAndValidity();
    });
  }

  private loadNotificationSettings(): void {
    this.isLoading = true;
    this.notificationSettingsService.getNotificationPreferences().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.settingsForm.patchValue({
            isDiscordNotificationEnabled: response.data.isDiscordNotificationEnabled,
            discordWebhookUrl: response.data.discordWebhookUrl || '',
            customBotName: response.data.customBotName || '',
            customAvatarUrl: response.data.customAvatarUrl || '',
            notificationProductIds: response.data.notificationProductIds || []
          });
        }
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading notification settings:', error);
        this.snackBar.open('Failed to load notification settings', 'Close', { duration: 5000 });
        this.isLoading = false;
      }
    });
  }

  private loadAvailableProducts(): void {
    this.notificationSettingsService.getAvailableProductsForNotification().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.availableProducts = response.data;
        }
      },
      error: (error) => {
        console.error('Error loading available products:', error);
        this.snackBar.open('Failed to load available products', 'Close', { duration: 5000 });
      }
    });
  }

  private loadSummary(): void {
    this.notificationSettingsService.getNotificationPreferencesSummary().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.summary = response.data;
        }
      },
      error: (error) => {
        console.error('Error loading notification summary:', error);
      }
    });
  }

  onProductSelectionChange(product: NotificationProductSelectionDto, event: any): void {
    product.isSelected = event.checked;
    
    const selectedIds = this.availableProducts
      .filter(p => p.isSelected)
      .map(p => p.productId);
    
    this.settingsForm.patchValue({
      notificationProductIds: selectedIds
    });
  }

  getSelectedProductsCount(): number {
    return this.availableProducts.filter(p => p.isSelected).length;
  }

  testWebhook(): void {
    const webhookUrl = this.settingsForm.get('discordWebhookUrl')?.value;
    if (!webhookUrl) {
      this.snackBar.open('Please enter a webhook URL first', 'Close', { duration: 3000 });
      return;
    }

    this.isTestingWebhook = true;
    const testDto = new TestDiscordWebhookDto({
      discordWebhookUrl: webhookUrl,
      customBotName: this.settingsForm.get('customBotName')?.value || undefined,
      customAvatarUrl: this.settingsForm.get('customAvatarUrl')?.value || undefined
    });

    this.notificationSettingsService.testDiscordWebhook(testDto).subscribe({
      next: (response) => {
        if (response.success) {
          this.snackBar.open('Test notification sent successfully! Check your Discord channel.', 'Close', { duration: 5000 });
        } else {
          this.snackBar.open(response.message || 'Failed to send test notification', 'Close', { duration: 5000 });
        }
        this.isTestingWebhook = false;
      },
      error: (error) => {
        console.error('Error testing webhook:', error);
        this.snackBar.open('Failed to send test notification. Please check your webhook URL.', 'Close', { duration: 5000 });
        this.isTestingWebhook = false;
      }
    });
  }

  onSave(): void {
    if (this.settingsForm.valid) {
      this.isSaving = true;
      const formValue = this.settingsForm.value;

      const updateDto = new UpdateUserNotificationPreferencesDto({
        isDiscordNotificationEnabled: formValue.isDiscordNotificationEnabled,
        discordWebhookUrl: formValue.discordWebhookUrl || undefined,
        customBotName: formValue.customBotName || undefined,
        customAvatarUrl: formValue.customAvatarUrl || undefined,
        notificationProductIds: formValue.notificationProductIds || []
      });

      this.notificationSettingsService.updateNotificationPreferences(updateDto).subscribe({
        next: (response) => {
          if (response.success) {
            this.snackBar.open('Notification settings saved successfully', 'Close', { duration: 3000 });
            this.loadSummary(); // Refresh summary
          } else {
            this.snackBar.open(response.message || 'Failed to save settings', 'Close', { duration: 5000 });
          }
          this.isSaving = false;
        },
        error: (error) => {
          console.error('Error saving notification settings:', error);
          this.snackBar.open('Failed to save notification settings', 'Close', { duration: 5000 });
          this.isSaving = false;
        }
      });
    } else {
      this.markFormGroupTouched();
    }
  }

  onCancel(): void {
    this.router.navigate(['/dashboard']);
  }

  private markFormGroupTouched(): void {
    Object.keys(this.settingsForm.controls).forEach(key => {
      const control = this.settingsForm.get(key);
      control?.markAsTouched();
    });
  }
}
