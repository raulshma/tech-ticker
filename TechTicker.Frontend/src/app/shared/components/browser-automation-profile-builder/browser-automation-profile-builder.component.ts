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

export interface BrowserAutomationAction {
  actionType: string;
  selector?: string;
  timeoutMs?: number;
  [key: string]: any;
}

export interface BrowserAutomationProfile {
  preferredBrowser: 'chromium' | 'firefox' | 'webkit';
  timeoutSeconds: number;
  userAgent?: string;
  proxy?: {
    host: string;
    port: number;
    username?: string;
    password?: string;
  };
  actions: BrowserAutomationAction[];
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
    MatMenuModule,
    MatToolbarModule,
    MatSidenavModule,
    MatListModule,
    MatError,
  ]
})
export class BrowserAutomationProfileBuilderComponent implements ControlValueAccessor, OnInit {
  @Input() parentForm?: FormGroup;

  profileForm: FormGroup;
  browsers = [
    { value: 'chromium', label: 'Chromium' },
    { value: 'firefox', label: 'Firefox' },
    { value: 'webkit', label: 'Webkit' }
  ];
  actionTypes = [
    { value: 'scroll', label: 'Scroll' },
    { value: 'click', label: 'Click' },
    { value: 'wait', label: 'Wait' },
    { value: 'waitForSelector', label: 'Wait for Selector' },
    { value: 'type', label: 'Type Text' },
    { value: 'evaluate', label: 'Evaluate JavaScript' },
    { value: 'screenshot', label: 'Screenshot' },
    { value: 'hover', label: 'Hover' },
    { value: 'selectOption', label: 'Select Option' },
    { value: 'setValue', label: 'Set Value (JS)' }
  ];
  validationError: string | null = null;
  showRawJson = false;
  rawJsonControl = new FormControl('');

  private onChange: any = () => {};
  private onTouched: any = () => {};

  constructor(private fb: FormBuilder) {
    this.profileForm = this.fb.group({
      preferredBrowser: ['chromium', Validators.required],
      timeoutSeconds: [30, [Validators.required, Validators.min(1)]],
      userAgent: [''],
      proxy: this.fb.group({
        host: [''],
        port: [null],
        username: [''],
        password: ['']
      }),
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
  }

  get actions(): FormArray {
    return this.profileForm.get('actions') as FormArray;
  }

  get proxyGroup(): FormGroup {
    return this.profileForm.get('proxy') as FormGroup;
  }

  addAction() {
    this.actions.push(this.fb.group({
      actionType: ['', Validators.required],
      selector: [''],
      timeoutMs: [null],
      value: [''],
      delayMs: [null]
    }));
  }

  removeAction(index: number) {
    this.actions.removeAt(index);
  }

  writeValue(obj: any): void {
    if (obj) {
      try {
        this.profileForm.patchValue(obj, { emitEvent: false });
        this.actions.clear();
        if (Array.isArray(obj.actions)) {
          obj.actions.forEach((a: any) => {
            this.actions.push(this.fb.group({
              actionType: [a.actionType, Validators.required],
              selector: [a.selector || ''],
              timeoutMs: [a.timeoutMs || null],
              value: [a.value || ''],
              delayMs: [a.delayMs || null]
            }));
          });
        }
        this.rawJsonControl.setValue(JSON.stringify(obj, null, 2), { emitEvent: false });
      } catch {
        // ignore
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
    const value = this.profileForm.value;
    
    // Always propagate the current value to prevent form resets
    this.onChange(value);
    this.rawJsonControl.setValue(JSON.stringify(value, null, 2), { emitEvent: false });
    
    // Update validation error display
    if (this.profileForm.valid) {
      this.validationError = null;
    } else {
      // Only show validation error if user has interacted with invalid fields
      const hasInvalidTouchedFields = this.hasInvalidTouchedFields();
      this.validationError = hasInvalidTouchedFields ? 'Please fix validation errors in the form.' : null;
    }
  }

  private hasInvalidTouchedFields(): boolean {
    if (this.profileForm.get('timeoutSeconds')?.invalid && this.profileForm.get('timeoutSeconds')?.touched) {
      return true;
    }
    
    // Check if any action has invalid required fields that have been touched
    const actionsArray = this.actions;
    for (let i = 0; i < actionsArray.length; i++) {
      const actionGroup = actionsArray.at(i);
      const actionTypeControl = actionGroup?.get('actionType');
      if (actionTypeControl?.invalid && actionTypeControl?.touched) {
        return true;
      }
    }
    
    return false;
  }

  toggleRawJson() {
    this.showRawJson = !this.showRawJson;
    if (this.showRawJson) {
      this.rawJsonControl.setValue(JSON.stringify(this.profileForm.value, null, 2), { emitEvent: false });
    } else {
      this.tryParseRawJson(this.rawJsonControl.value ?? '');
    }
  }

  tryParseRawJson(val: string) {
    try {
      const parsed = JSON.parse(val);
      this.writeValue(parsed);
      this.validationError = null;
      this.onChange(parsed);
    } catch {
      this.validationError = 'Invalid JSON.';
      this.onChange(null);
    }
  }

  getActionLabel(type: string): string {
    return this.actionTypes.find(a => a.value === type)?.label || type;
  }
} 