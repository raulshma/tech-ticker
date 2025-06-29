import { Component, forwardRef, Input, NO_ERRORS_SCHEMA, OnInit } from '@angular/core';
import { ControlValueAccessor, FormArray, FormBuilder, FormControl, FormGroup, NG_VALUE_ACCESSOR, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatOptionModule } from '@angular/material/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { MatMenuModule } from '@angular/material/menu';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatError } from '@angular/material/form-field';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatChipsModule } from '@angular/material/chips';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialogModule } from '@angular/material/dialog';
import { TechTickerApiClient, AiConfigurationDto, BrowserActionGenerationRequestDto, BrowserActionGenerationResponseDto } from '../../api/api-client';

export interface BrowserAutomationAction {
  actionType: string;
  selector?: string;
  repeat?: number;
  delayMs?: number;
  value?: string;
}

export interface BrowserAutomationProfile {
  preferredBrowser?: string; // "chromium", "firefox", "webkit"
  waitTimeMs?: number;
  actions?: BrowserAutomationAction[];
  timeoutSeconds?: number;
  userAgent?: string;
  headers?: { [key: string]: string };
  proxyServer?: string; // Full proxy URL like "http://proxy.example.com:8080"
  proxyUsername?: string;
  proxyPassword?: string;
}

@Component({
  selector: 'app-browser-automation-profile-builder',
  templateUrl: './browser-automation-profile-builder.component.html',
  styleUrls: ['./browser-automation-profile-builder.component.scss'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => BrowserAutomationProfileBuilderComponent),
      multi: true
    }
  ],
  schemas: [NO_ERRORS_SCHEMA],
  standalone: true,
  imports: [
    // Angular
    CommonModule,
    ReactiveFormsModule,
    // Material
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatOptionModule,
    MatButtonModule,
    MatIconModule,
    MatExpansionModule,
    MatTooltipModule,
    MatDividerModule,
    MatSlideToggleModule,
    MatChipsModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatDialogModule
  ]
})
export class BrowserAutomationProfileBuilderComponent implements ControlValueAccessor, OnInit {
  @Input() parentForm?: FormGroup;

  profileForm: FormGroup;
  browsers = [
    { value: 'chromium', label: 'Chromium (Default)' },
    { value: 'firefox', label: 'Firefox' },
    { value: 'webkit', label: 'WebKit (Safari)' }
  ];
  
  // Updated action types to match backend implementation
  actionTypes = [
    { value: 'scroll', label: 'Scroll Down', description: 'Scroll down by one viewport' },
    { value: 'click', label: 'Click Element', description: 'Click on an element by CSS selector' },
    { value: 'waitForSelector', label: 'Wait for Selector', description: 'Wait for an element to appear' },
    { value: 'type', label: 'Type Text', description: 'Type text into an input field' },
    { value: 'wait', label: 'Wait (Timeout)', description: 'Wait for a specified time' },
    { value: 'waitForTimeout', label: 'Wait for Timeout', description: 'Wait for a specified time (alternative)' },
    { value: 'screenshot', label: 'Take Screenshot', description: 'Capture a screenshot of the page' },
    { value: 'evaluate', label: 'Execute JavaScript', description: 'Run custom JavaScript code' },
    { value: 'hover', label: 'Hover Element', description: 'Hover over an element' },
    { value: 'selectOption', label: 'Select Option', description: 'Select an option from a dropdown' },
    { value: 'setValue', label: 'Set Value (JS)', description: 'Set input value using JavaScript' }
  ];
  
  validationError: string | null = null;
  showRawJson = false;
  rawJsonControl = new FormControl('');

  // Form fields for headers
  headersArray: FormArray;

  // AI Generation properties
  aiInstructions = new FormControl('');
  isGeneratingActions = false;
  hasAiConfiguration = false;
  aiError: string | null = null;

  private onChange: any = () => {};
  private onTouched: any = () => {};

  constructor(private fb: FormBuilder, private snackBar: MatSnackBar, private apiClient: TechTickerApiClient) {
    this.headersArray = this.fb.array([]);
    
    this.profileForm = this.fb.group({
      preferredBrowser: ['chromium'],
      timeoutSeconds: [30, [Validators.min(1)]],
      waitTimeMs: [null, [Validators.min(0)]],
      userAgent: [''],
      proxyServer: [''],
      proxyUsername: [''],
      proxyPassword: [''],
      headers: this.headersArray,
      actions: this.fb.array([])
    });
  }

  ngOnInit(): void {
    this.profileForm.valueChanges.subscribe(val => {
      if (!this.showRawJson) {
        this.propagateChange();
      }
    });
    this.rawJsonControl.valueChanges.subscribe(val => {
      if (this.showRawJson) {
        this.tryParseRawJson(val ?? '');
      }
    });
    
    // Check for AI configuration availability
    this.checkAiConfigurationAvailability();
  }

  get actions(): FormArray {
    return this.profileForm.get('actions') as FormArray;
  }

  get headers(): FormArray {
    return this.profileForm.get('headers') as FormArray;
  }

  addAction() {
    this.actions.push(this.fb.group({
      actionType: ['', Validators.required],
      selector: [''],
      repeat: [1, [Validators.min(1)]],
      delayMs: [null, [Validators.min(0)]],
      value: ['']
    }));
  }

  removeAction(index: number) {
    this.actions.removeAt(index);
  }

  addHeader() {
    this.headers.push(this.fb.group({
      key: ['', Validators.required],
      value: ['', Validators.required]
    }));
  }

  removeHeader(index: number) {
    this.headers.removeAt(index);
  }

  writeValue(obj: any): void {
    if (obj) {
      try {
        // Clear existing form arrays
        this.actions.clear();
        this.headers.clear();
        
        // Set basic form values
        this.profileForm.patchValue({
          preferredBrowser: obj.preferredBrowser || 'chromium',
          timeoutSeconds: obj.timeoutSeconds || 30,
          waitTimeMs: obj.waitTimeMs || null,
          userAgent: obj.userAgent || '',
          proxyServer: obj.proxyServer || '',
          proxyUsername: obj.proxyUsername || '',
          proxyPassword: obj.proxyPassword || ''
        }, { emitEvent: false });
        
        // Populate actions
        if (Array.isArray(obj.actions)) {
          obj.actions.forEach((action: any) => {
            this.actions.push(this.fb.group({
              actionType: [action.actionType || '', Validators.required],
              selector: [action.selector || ''],
              repeat: [action.repeat || 1, [Validators.min(1)]],
              delayMs: [action.delayMs || null, [Validators.min(0)]],
              value: [action.value || '']
            }));
          });
        }
        
        // Populate headers
        if (obj.headers && typeof obj.headers === 'object') {
          Object.entries(obj.headers).forEach(([key, value]) => {
            this.headers.push(this.fb.group({
              key: [key, Validators.required],
              value: [value, Validators.required]
            }));
          });
        }
        
        this.rawJsonControl.setValue(JSON.stringify(obj, null, 2), { emitEvent: false });
      } catch (error) {
        console.error('Error parsing browser automation profile:', error);
      }
    }
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  setDisabledState?(isDisabled: boolean): void {
    if (isDisabled) {
      this.profileForm.disable();
      this.rawJsonControl.disable();
    } else {
      this.profileForm.enable();
      this.rawJsonControl.enable();
    }
  }

  propagateChange() {
    const formValue = this.profileForm.value;
    
    // Transform form data to match backend structure
    const profile: BrowserAutomationProfile = {
      preferredBrowser: formValue.preferredBrowser || undefined,
      timeoutSeconds: formValue.timeoutSeconds || undefined,
      waitTimeMs: formValue.waitTimeMs || undefined,
      userAgent: formValue.userAgent || undefined,
      proxyServer: formValue.proxyServer || undefined,
      proxyUsername: formValue.proxyUsername || undefined,
      proxyPassword: formValue.proxyPassword || undefined
    };
    
    // Transform headers array to dictionary
    if (formValue.headers && Array.isArray(formValue.headers)) {
      const headersDict: { [key: string]: string } = {};
      formValue.headers.forEach((header: any) => {
        if (header.key && header.value) {
          headersDict[header.key] = header.value;
        }
      });
      if (Object.keys(headersDict).length > 0) {
        profile.headers = headersDict;
      }
    }
    
    // Transform actions array
    if (formValue.actions && Array.isArray(formValue.actions)) {
      profile.actions = formValue.actions.map((action: any) => ({
        actionType: action.actionType,
        selector: action.selector || undefined,
        repeat: action.repeat || undefined,
        delayMs: action.delayMs || undefined,
        value: action.value || undefined
      })).filter((action: any) => action.actionType); // Only include actions with actionType
    }
    
    // Remove undefined values to clean up the JSON
    const cleanProfile = this.removeUndefinedValues(profile);
    
    // Always propagate the current value to prevent form resets
    this.onChange(cleanProfile);
    this.rawJsonControl.setValue(JSON.stringify(cleanProfile, null, 2), { emitEvent: false });
    
    // Update validation error display
    if (this.profileForm.valid) {
      this.validationError = null;
    } else {
      const hasInvalidTouchedFields = this.hasInvalidTouchedFields();
      this.validationError = hasInvalidTouchedFields ? 'Please fix validation errors in the form.' : null;
    }
  }

  private removeUndefinedValues(obj: any): any {
    if (Array.isArray(obj)) {
      return obj.map(item => this.removeUndefinedValues(item));
    } else if (obj !== null && typeof obj === 'object') {
      const cleaned: any = {};
      for (const [key, value] of Object.entries(obj)) {
        if (value !== undefined && value !== null && value !== '') {
          cleaned[key] = this.removeUndefinedValues(value);
        }
      }
      return Object.keys(cleaned).length > 0 ? cleaned : undefined;
    }
    return obj;
  }

  private hasInvalidTouchedFields(): boolean {
    // Check basic form fields
    const basicFields = ['timeoutSeconds', 'waitTimeMs'];
    for (const field of basicFields) {
      const control = this.profileForm.get(field);
      if (control?.invalid && control?.touched) {
        return true;
      }
    }
    
    // Check actions array
    const actionsArray = this.actions;
    for (let i = 0; i < actionsArray.length; i++) {
      const actionGroup = actionsArray.at(i);
      const actionTypeControl = actionGroup?.get('actionType');
      const repeatControl = actionGroup?.get('repeat');
      const delayMsControl = actionGroup?.get('delayMs');
      
      if ((actionTypeControl?.invalid && actionTypeControl?.touched) ||
          (repeatControl?.invalid && repeatControl?.touched) ||
          (delayMsControl?.invalid && delayMsControl?.touched)) {
        return true;
      }
    }
    
    // Check headers array
    const headersArray = this.headers;
    for (let i = 0; i < headersArray.length; i++) {
      const headerGroup = headersArray.at(i);
      const keyControl = headerGroup?.get('key');
      const valueControl = headerGroup?.get('value');
      
      if ((keyControl?.invalid && keyControl?.touched) ||
          (valueControl?.invalid && valueControl?.touched)) {
        return true;
      }
    }
    
    return false;
  }

  toggleRawJson() {
    this.showRawJson = !this.showRawJson;
    if (this.showRawJson) {
      const currentValue = this.profileForm.value;
      const transformedValue = this.transformFormToProfile(currentValue);
      this.rawJsonControl.setValue(JSON.stringify(transformedValue, null, 2), { emitEvent: false });
    } else {
      this.tryParseRawJson(this.rawJsonControl.value ?? '');
    }
  }

  private transformFormToProfile(formValue: any): BrowserAutomationProfile {
    const profile: BrowserAutomationProfile = {};
    
    if (formValue.preferredBrowser) profile.preferredBrowser = formValue.preferredBrowser;
    if (formValue.timeoutSeconds) profile.timeoutSeconds = formValue.timeoutSeconds;
    if (formValue.waitTimeMs) profile.waitTimeMs = formValue.waitTimeMs;
    if (formValue.userAgent) profile.userAgent = formValue.userAgent;
    if (formValue.proxyServer) profile.proxyServer = formValue.proxyServer;
    if (formValue.proxyUsername) profile.proxyUsername = formValue.proxyUsername;
    if (formValue.proxyPassword) profile.proxyPassword = formValue.proxyPassword;
    
    // Transform headers
    if (formValue.headers && Array.isArray(formValue.headers)) {
      const headersDict: { [key: string]: string } = {};
      formValue.headers.forEach((header: any) => {
        if (header.key && header.value) {
          headersDict[header.key] = header.value;
        }
      });
      if (Object.keys(headersDict).length > 0) {
        profile.headers = headersDict;
      }
    }
    
    // Transform actions
    if (formValue.actions && Array.isArray(formValue.actions)) {
      profile.actions = formValue.actions
        .filter((action: any) => action.actionType)
        .map((action: any) => {
          const transformedAction: BrowserAutomationAction = {
            actionType: action.actionType
          };
          if (action.selector) transformedAction.selector = action.selector;
          if (action.repeat && action.repeat !== 1) transformedAction.repeat = action.repeat;
          if (action.delayMs) transformedAction.delayMs = action.delayMs;
          if (action.value) transformedAction.value = action.value;
          return transformedAction;
        });
    }
    
    return profile;
  }

  tryParseRawJson(val: string) {
    try {
      const parsed = JSON.parse(val);
      this.writeValue(parsed);
      this.validationError = null;
      this.onChange(parsed);
    } catch {
      this.validationError = 'Invalid JSON format.';
      this.onChange(null);
    }
  }

  getActionTypeInfo(actionType: string): { label: string; description: string } {
    const actionInfo = this.actionTypes.find(a => a.value === actionType);
    return {
      label: actionInfo?.label || actionType,
      description: actionInfo?.description || ''
    };
  }

  getActionLabel(type: string): string {
    return this.actionTypes.find(a => a.value === type)?.label || type;
  }

  // Helper method to determine if action needs specific fields
  actionNeedsSelector(actionType: string): boolean {
    return ['click', 'waitForSelector', 'type', 'hover', 'selectOption', 'setValue'].includes(actionType);
  }

  actionNeedsValue(actionType: string): boolean {
    return ['type', 'evaluate', 'screenshot', 'selectOption', 'setValue'].includes(actionType);
  }

  actionNeedsDelay(actionType: string): boolean {
    return ['wait', 'waitForTimeout'].includes(actionType);
  }

  getValuePlaceholder(actionType: string): string {
    switch (actionType) {
      case 'type': return 'Text to type';
      case 'evaluate': return 'JavaScript code';
      case 'screenshot': return 'File path (optional)';
      case 'selectOption': return 'Option value';
      case 'setValue': return 'Value to set';
      default: return 'Value';
    }
  }

  /**
   * Copy the current JSON configuration to clipboard
   */
  copyToClipboard(): void {
    try {
      // Get the current form value and transform it to the profile format
      const currentValue = this.profileForm.value;
      const transformedValue = this.transformFormToProfile(currentValue);
      const jsonString = JSON.stringify(transformedValue, null, 2);
      
      // Use the modern Clipboard API if available
      if (navigator.clipboard && window.isSecureContext) {
        navigator.clipboard.writeText(jsonString).then(() => {
          this.showCopySuccessMessage();
        }).catch((err) => {
          console.error('Failed to copy to clipboard:', err);
          this.fallbackCopyToClipboard(jsonString);
        });
      } else {
        // Fallback for older browsers or non-secure contexts
        this.fallbackCopyToClipboard(jsonString);
      }
    } catch (error) {
      console.error('Error copying to clipboard:', error);
      this.snackBar.open('Failed to copy configuration to clipboard', 'Close', {
        duration: 3000,
        panelClass: ['error-snackbar']
      });
    }
  }

  /**
   * Fallback method for copying to clipboard in older browsers
   */
  private fallbackCopyToClipboard(text: string): void {
    try {
      // Create a temporary textarea element
      const textArea = document.createElement('textarea');
      textArea.value = text;
      textArea.style.position = 'fixed';
      textArea.style.left = '-999999px';
      textArea.style.top = '-999999px';
      document.body.appendChild(textArea);
      
      // Select and copy the text
      textArea.focus();
      textArea.select();
      
      const successful = document.execCommand('copy');
      document.body.removeChild(textArea);
      
      if (successful) {
        this.showCopySuccessMessage();
      } else {
        throw new Error('execCommand failed');
      }
    } catch (err) {
      console.error('Fallback copy failed:', err);
      this.snackBar.open('Failed to copy configuration to clipboard', 'Close', {
        duration: 3000,
        panelClass: ['error-snackbar']
      });
    }
  }

  /**
   * Show success message when copy operation is successful
   */
  private showCopySuccessMessage(): void {
    this.snackBar.open('Configuration copied to clipboard!', 'Close', {
      duration: 3000,
      panelClass: ['success-snackbar'],
      horizontalPosition: 'center',
      verticalPosition: 'bottom'
    });
  }

  /**
   * Check if AI configuration is available
   */
  private checkAiConfigurationAvailability(): void {
    this.apiClient.getDefaultConfiguration().subscribe({
      next: (response) => {
        this.hasAiConfiguration = !!(response.success && response.data);
      },
      error: () => {
        this.hasAiConfiguration = false;
      }
    });
  }

  /**
   * Generate browser actions using AI
   */
  async generateActionsWithAI(): Promise<void> {
    const instructions = this.aiInstructions.value?.trim();
    if (!instructions) {
      this.snackBar.open('Please enter instructions for the AI to generate actions', 'Close', {
        duration: 3000,
        panelClass: ['error-snackbar']
      });
      return;
    }

    this.isGeneratingActions = true;
    this.aiError = null;

    try {
      const request = new BrowserActionGenerationRequestDto({
        instructions: instructions,
        aiConfigurationId: undefined // Use default configuration
      });

      const response = await this.apiClient.generateBrowserActions(request).toPromise();
      
      if (response?.success && response.data?.actions && response.data.actions.length > 0) {
        // Clear existing actions
        this.actions.clear();
        
        // Add generated actions to the form
        response.data.actions.forEach(action => {
          this.actions.push(this.fb.group({
            actionType: [action.actionType || '', Validators.required],
            selector: [action.selector || ''],
            repeat: [action.repeat || 1, [Validators.min(1)]],
            delayMs: [action.delayMs || null, [Validators.min(0)]],
            value: [action.value || '']
          }));
        });

        // Clear the instructions
        this.aiInstructions.setValue('');
        
        // Show success message
        this.snackBar.open(`Generated ${response.data.actions.length} actions successfully!`, 'Close', {
          duration: 4000,
          panelClass: ['success-snackbar']
        });
      } else {
        this.aiError = response?.data?.errorMessage || response?.message || 'Failed to generate actions. Please try again.';
      }
    } catch (error: any) {
      console.error('AI generation error:', error);
      this.aiError = error?.error?.message || error?.message || 'An error occurred while generating actions';
    } finally {
      this.isGeneratingActions = false;
    }
  }

  /**
   * Clear AI instructions
   */
  clearAiInstructions(): void {
    this.aiInstructions.setValue('');
    this.aiError = null;
  }
} 