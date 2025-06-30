import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatButtonModule } from '@angular/material/button';
import { MatTabsModule } from '@angular/material/tabs';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { MatCardModule } from '@angular/material/card';
import { MatExpansionModule } from '@angular/material/expansion';

export interface AdvancedTestConfigDialogData {
  testOptions: any;
}

@Component({
  selector: 'app-advanced-test-config-dialog',
  templateUrl: './advanced-test-config-dialog.component.html',
  styleUrls: ['./advanced-test-config-dialog.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatButtonModule,
    MatTabsModule,
    MatIconModule,
    MatTooltipModule,
    MatDividerModule,
    MatChipsModule,
    MatCardModule,
    MatExpansionModule
  ]
})
export class AdvancedTestConfigDialogComponent implements OnInit {
  configForm: FormGroup;
  
  browserEngines = [
    { value: 'chromium', label: 'Chromium (Recommended)', description: 'Google Chrome/Microsoft Edge compatible' },
    { value: 'firefox', label: 'Firefox', description: 'Mozilla Firefox compatible' },
    { value: 'webkit', label: 'WebKit', description: 'Safari compatible' }
  ];

  videoQualities = [
    { value: 'low', label: 'Low (480p)', description: 'Smaller file size, faster processing' },
    { value: 'medium', label: 'Medium (720p)', description: 'Balanced quality and size' },
    { value: 'high', label: 'High (1080p)', description: 'Best quality, larger file size' }
  ];

  screenshotFormats = [
    { value: 'png', label: 'PNG', description: 'Lossless, larger file size' },
    { value: 'jpeg', label: 'JPEG', description: 'Compressed, smaller file size' }
  ];

  constructor(
    private fb: FormBuilder,
    public dialogRef: MatDialogRef<AdvancedTestConfigDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: AdvancedTestConfigDialogData
  ) {
    this.configForm = this.createForm();
  }

  ngOnInit(): void {
    if (this.data.testOptions) {
      this.populateForm();
    }
  }

  private createForm(): FormGroup {
    return this.fb.group({
      // Browser Configuration
      browserEngine: ['chromium'],
      headless: [false],
      slowMotion: [0, [Validators.min(0), Validators.max(5000)]],
      
      // Video Recording
      enableVideoRecording: [false],
      recordVideo: [false], // Legacy support
      videoQuality: ['medium'],
      
      // Screenshots
      captureScreenshots: [true],
      screenshotFormat: ['png'],
      screenshotQuality: [80, [Validators.min(1), Validators.max(100)]],
      
      // Logging Options
      enableNetworkLogging: [true],
      enableConsoleLogging: [true],
      enablePerformanceLogging: [true],
      enableHAR: [false],
      enableTrace: [false],
      
      // Viewport Settings
      viewportWidth: [1920, [Validators.min(100), Validators.max(3840)]],
      viewportHeight: [1080, [Validators.min(100), Validators.max(2160)]],
      deviceEmulation: ['desktop'],
      userAgent: [''],
      
      // Timeout Settings
      testTimeoutMs: [60000, [Validators.min(1000), Validators.max(300000)]],
      actionTimeoutMs: [30000, [Validators.min(1000), Validators.max(120000)]],
      navigationTimeoutMs: [30000, [Validators.min(1000), Validators.max(120000)]],
      
      // Proxy Settings
      proxyEnabled: [false],
      proxyServer: [''],
      proxyUsername: [''],
      proxyPassword: [''],
      
      // Custom Headers
      customHeaders: this.fb.array([])
    });
  }

  private populateForm(): void {
    const options = this.data.testOptions;
    
    this.configForm.patchValue({
      browserEngine: options.browserEngine || 'chromium',
      headless: options.headless || false,
      slowMotion: options.slowMotion || 0,
      enableVideoRecording: options.enableVideoRecording || false,
      recordVideo: options.recordVideo || false,
      videoQuality: options.videoQuality || 'medium',
      captureScreenshots: options.captureScreenshots || true,
      screenshotFormat: options.screenshotFormat || 'png',
      screenshotQuality: options.screenshotQuality || 80,
      enableNetworkLogging: options.enableNetworkLogging || true,
      enableConsoleLogging: options.enableConsoleLogging || true,
      enablePerformanceLogging: options.enablePerformanceLogging || true,
      enableHAR: options.enableHAR || false,
      enableTrace: options.enableTrace || false,
      viewportWidth: options.viewportWidth || 1920,
      viewportHeight: options.viewportHeight || 1080,
      deviceEmulation: options.deviceEmulation || 'desktop',
      userAgent: options.userAgent || '',
      testTimeoutMs: options.testTimeoutMs || 60000,
      actionTimeoutMs: options.actionTimeoutMs || 30000,
      navigationTimeoutMs: options.navigationTimeoutMs || 30000,
      proxyEnabled: options.proxySettings?.enabled || false,
      proxyServer: options.proxySettings?.server || '',
      proxyUsername: options.proxySettings?.username || '',
      proxyPassword: options.proxySettings?.password || ''
    });

    // Populate custom headers
    if (options.customHeaders) {
      const headersArray = this.configForm.get('customHeaders') as FormArray;
      Object.entries(options.customHeaders).forEach(([key, value]) => {
        headersArray.push(this.fb.group({
          key: [key, Validators.required],
          value: [value, Validators.required]
        }));
      });
    }
  }

  get customHeadersArray(): FormArray {
    return this.configForm.get('customHeaders') as FormArray;
  }

  addCustomHeader(): void {
    this.customHeadersArray.push(this.fb.group({
      key: ['', Validators.required],
      value: ['', Validators.required]
    }));
  }

  removeCustomHeader(index: number): void {
    this.customHeadersArray.removeAt(index);
  }

  onVideoRecordingToggle(): void {
    const enableVideoRecording = this.configForm.get('enableVideoRecording')?.value;
    // Sync with legacy recordVideo field
    this.configForm.patchValue({
      recordVideo: enableVideoRecording
    });
  }

  onProxyToggle(): void {
    const proxyEnabled = this.configForm.get('proxyEnabled')?.value;
    if (!proxyEnabled) {
      // Clear proxy fields when disabled
      this.configForm.patchValue({
        proxyServer: '',
        proxyUsername: '',
        proxyPassword: ''
      });
    }
  }

  onSave(): void {
    if (this.configForm.valid) {
      const formValue = this.configForm.value;
      
      // Convert form data to test options format
      const updatedOptions = {
        ...this.data.testOptions,
        browserEngine: formValue.browserEngine,
        headless: formValue.headless,
        slowMotion: formValue.slowMotion,
        enableVideoRecording: formValue.enableVideoRecording,
        recordVideo: formValue.recordVideo,
        videoQuality: formValue.videoQuality,
        captureScreenshots: formValue.captureScreenshots,
        screenshotFormat: formValue.screenshotFormat,
        screenshotQuality: formValue.screenshotQuality,
        enableNetworkLogging: formValue.enableNetworkLogging,
        enableConsoleLogging: formValue.enableConsoleLogging,
        enablePerformanceLogging: formValue.enablePerformanceLogging,
        enableHAR: formValue.enableHAR,
        enableTrace: formValue.enableTrace,
        viewportWidth: formValue.viewportWidth,
        viewportHeight: formValue.viewportHeight,
        deviceEmulation: formValue.deviceEmulation,
        userAgent: formValue.userAgent,
        testTimeoutMs: formValue.testTimeoutMs,
        actionTimeoutMs: formValue.actionTimeoutMs,
        navigationTimeoutMs: formValue.navigationTimeoutMs,
        proxySettings: {
          enabled: formValue.proxyEnabled,
          server: formValue.proxyServer,
          username: formValue.proxyUsername,
          password: formValue.proxyPassword
        },
        customHeaders: formValue.customHeaders.reduce((headers: any, header: any) => {
          if (header.key && header.value) {
            headers[header.key] = header.value;
          }
          return headers;
        }, {})
      };

      this.dialogRef.close(updatedOptions);
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  resetToDefaults(): void {
    this.configForm.reset();
    this.configForm.patchValue({
      browserEngine: 'chromium',
      headless: false,
      slowMotion: 0,
      enableVideoRecording: false,
      recordVideo: false,
      videoQuality: 'medium',
      captureScreenshots: true,
      screenshotFormat: 'png',
      screenshotQuality: 80,
      enableNetworkLogging: true,
      enableConsoleLogging: true,
      enablePerformanceLogging: true,
      enableHAR: false,
      enableTrace: false,
      viewportWidth: 1920,
      viewportHeight: 1080,
      deviceEmulation: 'desktop',
      userAgent: '',
      testTimeoutMs: 60000,
      actionTimeoutMs: 30000,
      navigationTimeoutMs: 30000,
      proxyEnabled: false,
      proxyServer: '',
      proxyUsername: '',
      proxyPassword: ''
    });
    
    // Clear custom headers
    while (this.customHeadersArray.length > 0) {
      this.customHeadersArray.removeAt(0);
    }
  }
} 