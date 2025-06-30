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

  getSelectedProductsCount(): number {
    return this.availableProducts.filter(p => p.isSelected).length;
  }

  getMaxProductsAllowed(): number {
    return this.summary?.maxProductsAllowed || 5;
  }

  getProductSelectionProgress(): number {
    const selected = this.getSelectedProductsCount();
    const max = this.getMaxProductsAllowed();
    return (selected / max) * 100;
  }

  isMaxProductsReached(): boolean {
    return this.getSelectedProductsCount() >= this.getMaxProductsAllowed();
  }

  getProductsByCategory(): { [category: string]: NotificationProductSelectionDto[] } {
    const grouped: { [category: string]: NotificationProductSelectionDto[] } = {};
    
    this.availableProducts.forEach(product => {
      const category = product.categoryName || 'Uncategorized';
      if (!grouped[category]) {
        grouped[category] = [];
      }
      grouped[category].push(product);
    });

    return grouped;
  }

  selectAllProductsInCategory(categoryProducts: NotificationProductSelectionDto[]): void {
    const availableSlots = this.getMaxProductsAllowed() - this.getSelectedProductsCount();
    let slotsUsed = 0;

    categoryProducts.forEach(product => {
      if (!product.isSelected && slotsUsed < availableSlots) {
        product.isSelected = true;
        slotsUsed++;
      }
    });

    this.updateSelectedProductIds();
  }

  deselectAllProductsInCategory(categoryProducts: NotificationProductSelectionDto[]): void {
    categoryProducts.forEach(product => {
      product.isSelected = false;
    });

    this.updateSelectedProductIds();
  }

  private updateSelectedProductIds(): void {
    const selectedIds = this.availableProducts
      .filter(p => p.isSelected)
      .map(p => p.productId);
    
    this.settingsForm.patchValue({
      notificationProductIds: selectedIds
    });
  }

  onProductSelectionChange(product: NotificationProductSelectionDto, event: any): void {
    if (event.checked && this.isMaxProductsReached()) {
      event.source.checked = false;
      this.snackBar.open(
        `Maximum of ${this.getMaxProductsAllowed()} products can be selected for notifications`, 
        'Close', 
        { duration: 4000 }
      );
      return;
    }

    product.isSelected = event.checked;
    this.updateSelectedProductIds();

    // Show helpful message when approaching limit
    const selected = this.getSelectedProductsCount();
    const max = this.getMaxProductsAllowed();
    if (selected === max - 1) {
      this.snackBar.open(
        `You can select 1 more product for notifications`, 
        'Close', 
        { duration: 3000 }
      );
    }
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
