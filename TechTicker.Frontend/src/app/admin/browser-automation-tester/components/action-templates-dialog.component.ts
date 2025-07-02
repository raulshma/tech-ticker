import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatBadgeModule } from '@angular/material/badge';

export interface ActionTemplate {
  id: string;
  name: string;
  description?: string;
  category: string;
  actions: BrowserAutomationAction[];
  createdAt: Date;
  lastUsed?: Date;
  usageCount: number;
}

export interface BrowserAutomationAction {
  actionType: string;
  selector?: string;
  repeat?: number;
  delayMs?: number;
  value?: string;
}

@Component({
  selector: 'app-action-templates-dialog',
  templateUrl: './action-templates-dialog.component.html',
  styleUrls: ['./action-templates-dialog.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCardModule,
    MatChipsModule,
    MatTooltipModule,
    MatSnackBarModule,
    MatDividerModule,
    MatExpansionModule,
    MatListModule,
    MatMenuModule,
    MatBadgeModule
  ]
})
export class ActionTemplatesDialogComponent implements OnInit {
  templates: ActionTemplate[] = [];
  filteredTemplates: ActionTemplate[] = [];
  selectedTemplate: ActionTemplate | null = null;
  searchTerm = '';
  selectedCategory = 'all';
  
  // Form for creating/editing templates
  templateForm: FormGroup;
  isEditing = false;
  editingTemplateId: string | null = null;
  
  // Categories for organizing templates
  categories = [
    { value: 'navigation', label: 'Navigation', icon: 'navigation' },
    { value: 'form-filling', label: 'Form Filling', icon: 'edit' },
    { value: 'clicking', label: 'Clicking', icon: 'touch_app' },
    { value: 'waiting', label: 'Waiting', icon: 'schedule' },
    { value: 'scrolling', label: 'Scrolling', icon: 'unfold_more' },
    { value: 'screenshots', label: 'Screenshots', icon: 'camera_alt' },
    { value: 'custom', label: 'Custom', icon: 'code' },
    { value: 'ecommerce', label: 'E-commerce', icon: 'shopping_cart' },
    { value: 'social-media', label: 'Social Media', icon: 'share' },
    { value: 'testing', label: 'Testing', icon: 'bug_report' }
  ];

  // Action types for reference - Complete list from backend
  actionTypes = [
    // Navigation Actions
    { value: 'navigate', label: 'Navigate to URL', icon: 'navigation' },
    { value: 'goto', label: 'Go to URL', icon: 'navigation' },
    { value: 'url', label: 'Open URL', icon: 'navigation' },
    { value: 'reload', label: 'Reload Page', icon: 'refresh' },
    { value: 'refresh', label: 'Refresh Page', icon: 'refresh' },
    { value: 'goback', label: 'Go Back', icon: 'arrow_back' },
    { value: 'goforward', label: 'Go Forward', icon: 'arrow_forward' },
    
    // Clicking Actions
    { value: 'click', label: 'Click Element', icon: 'touch_app' },
    { value: 'doubleclick', label: 'Double Click', icon: 'mouse' },
    { value: 'rightclick', label: 'Right Click', icon: 'mouse' },
    
    // Input Actions
    { value: 'type', label: 'Type Text', icon: 'keyboard' },
    { value: 'clear', label: 'Clear Input', icon: 'clear' },
    { value: 'setValue', label: 'Set Value (JS)', icon: 'input' },
    { value: 'press', label: 'Press Key', icon: 'keyboard' },
    { value: 'upload', label: 'Upload File', icon: 'upload_file' },
    
    // Focus Actions
    { value: 'focus', label: 'Focus Element', icon: 'center_focus_strong' },
    { value: 'blur', label: 'Blur Element', icon: 'blur_on' },
    { value: 'hover', label: 'Hover Element', icon: 'mouse' },
    
    // Wait Actions
    { value: 'wait', label: 'Wait (Timeout)', icon: 'timer' },
    { value: 'waitForTimeout', label: 'Wait for Timeout', icon: 'timer' },
    { value: 'waitForSelector', label: 'Wait for Selector', icon: 'schedule' },
    { value: 'waitForNavigation', label: 'Wait for Navigation', icon: 'hourglass_empty' },
    { value: 'waitForLoadState', label: 'Wait for Load State', icon: 'hourglass_full' },
    
    // Scroll Actions
    { value: 'scroll', label: 'Scroll Down', icon: 'unfold_more' },
    
    // Selection Actions
    { value: 'selectOption', label: 'Select Option', icon: 'arrow_drop_down' },
    
    // Media Actions
    { value: 'screenshot', label: 'Take Screenshot', icon: 'camera_alt' },
    
    // JavaScript Actions
    { value: 'evaluate', label: 'Execute JavaScript', icon: 'code' },
    
    // Drag & Drop Actions
    { value: 'drag', label: 'Drag and Drop', icon: 'drag_indicator' },
    
    // Window Management
    { value: 'maximize', label: 'Maximize Window', icon: 'fullscreen' },
    { value: 'minimize', label: 'Minimize Window', icon: 'fullscreen_exit' },
    { value: 'fullscreen', label: 'Enter Fullscreen', icon: 'fullscreen' },
    { value: 'newtab', label: 'New Tab', icon: 'tab' },
    { value: 'newpage', label: 'New Page', icon: 'tab' },
    { value: 'closetab', label: 'Close Tab', icon: 'close' },
    { value: 'closepage', label: 'Close Page', icon: 'close' },
    { value: 'switchwindow', label: 'Switch Window', icon: 'swap_horiz' },
    { value: 'switchtab', label: 'Switch Tab', icon: 'swap_horiz' },
    
    // Frame Actions
    { value: 'switchframe', label: 'Switch Frame', icon: 'web_asset' },
    { value: 'switchiframe', label: 'Switch iFrame', icon: 'web_asset' },
    
    // Alert Actions
    { value: 'alert', label: 'Handle Alert', icon: 'warning' },
    { value: 'acceptalert', label: 'Accept Alert', icon: 'check_circle' },
    { value: 'dismissalert', label: 'Dismiss Alert', icon: 'cancel' },
    
    // Cookie Actions
    { value: 'getcookies', label: 'Get Cookies', icon: 'cookie' },
    { value: 'setcookies', label: 'Set Cookies', icon: 'cookie' },
    { value: 'deletecookies', label: 'Delete Cookies', icon: 'delete_sweep' },
    
    // Style & Script Injection
    { value: 'addstylesheet', label: 'Add Stylesheet', icon: 'style' },
    { value: 'addscript', label: 'Add Script', icon: 'code' },
    
    // Device Emulation
    { value: 'emulatedevice', label: 'Emulate Device', icon: 'smartphone' }
  ];

  constructor(
    private fb: FormBuilder,
    private snackBar: MatSnackBar,
    public dialogRef: MatDialogRef<ActionTemplatesDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { currentActions?: BrowserAutomationAction[], testUrl?: string }
  ) {
    this.templateForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      description: [''],
      category: ['custom', Validators.required],
      actions: this.fb.array([])
    });
  }

  ngOnInit(): void {
    this.loadTemplates();
    this.filterTemplates();
    
    // If current actions are provided, populate the form
    if (this.data?.currentActions?.length) {
      this.populateFormWithActions(this.data.currentActions);
    }
  }

  get actions() {
    return this.templateForm.get('actions') as any;
  }

  loadTemplates(): void {
    const stored = localStorage.getItem('browser-automation-action-templates');
    if (stored) {
      try {
        this.templates = JSON.parse(stored).map((t: any) => ({
          ...t,
          createdAt: new Date(t.createdAt),
          lastUsed: t.lastUsed ? new Date(t.lastUsed) : undefined
        }));
      } catch (error) {
        console.error('Error loading templates:', error);
        this.templates = [];
      }
    }
  }

  saveTemplates(): void {
    localStorage.setItem('browser-automation-action-templates', JSON.stringify(this.templates));
  }

  filterTemplates(): void {
    this.filteredTemplates = this.templates.filter(template => {
      const matchesSearch = !this.searchTerm || 
        template.name.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        template.description?.toLowerCase().includes(this.searchTerm.toLowerCase());
      
      const matchesCategory = this.selectedCategory === 'all' || template.category === this.selectedCategory;
      
      return matchesSearch && matchesCategory;
    });
  }

  onSearchChange(): void {
    this.filterTemplates();
  }

  onCategoryChange(): void {
    this.filterTemplates();
  }

  createNewTemplate(): void {
    this.isEditing = true;
    this.editingTemplateId = null;
    this.templateForm.reset({
      category: 'custom',
      actions: []
    });
    this.actions.clear();
  }

  editTemplate(template: ActionTemplate): void {
    this.isEditing = true;
    this.editingTemplateId = template.id;
    this.templateForm.patchValue({
      name: template.name,
      description: template.description,
      category: template.category
    });
    
    // Clear and populate actions
    this.actions.clear();
    template.actions.forEach(action => {
      this.actions.push(this.fb.group({
        actionType: [action.actionType, Validators.required],
        selector: [action.selector || ''],
        repeat: [action.repeat || 1, [Validators.min(1)]],
        delayMs: [action.delayMs || null, [Validators.min(0)]],
        value: [action.value || '']
      }));
    });
  }

  deleteTemplate(template: ActionTemplate): void {
    if (confirm(`Are you sure you want to delete the template "${template.name}"?`)) {
      this.templates = this.templates.filter(t => t.id !== template.id);
      this.saveTemplates();
      this.filterTemplates();
      this.snackBar.open('Template deleted successfully', 'Close', { duration: 3000 });
    }
  }

  saveTemplate(): void {
    if (this.templateForm.valid) {
      const formValue = this.templateForm.value;
      const templateActions = this.actions.value.map((action: any) => ({
        actionType: action.actionType,
        selector: action.selector || undefined,
        repeat: action.repeat || undefined,
        delayMs: action.delayMs || undefined,
        value: action.value || undefined
      })).filter((action: any) => action.actionType);

      if (templateActions.length === 0) {
        this.snackBar.open('Please add at least one action to the template', 'Close', { duration: 3000 });
        return;
      }

      if (this.editingTemplateId) {
        // Update existing template
        const index = this.templates.findIndex(t => t.id === this.editingTemplateId);
        if (index !== -1) {
          this.templates[index] = {
            ...this.templates[index],
            name: formValue.name,
            description: formValue.description,
            category: formValue.category,
            actions: templateActions
          };
        }
      } else {
        // Create new template
        const newTemplate: ActionTemplate = {
          id: this.generateId(),
          name: formValue.name,
          description: formValue.description,
          category: formValue.category,
          actions: templateActions,
          createdAt: new Date(),
          usageCount: 0
        };
        this.templates.unshift(newTemplate);
      }

      this.saveTemplates();
      this.filterTemplates();
      this.cancelEdit();
      this.snackBar.open('Template saved successfully', 'Close', { duration: 3000 });
    }
  }

  cancelEdit(): void {
    this.isEditing = false;
    this.editingTemplateId = null;
    this.templateForm.reset();
    this.actions.clear();
  }

  useTemplate(template: ActionTemplate): void {
    // Update usage statistics
    template.lastUsed = new Date();
    template.usageCount++;
    this.saveTemplates();
    
    // Process template actions to handle empty navigate URLs
    const processedActions = template.actions.map(action => {
      if (action.actionType === 'navigate' && !action.value) {
        // Empty navigate action will be handled by the parent component to use the current test URL
        return { ...action, value: '' };
      }
      return action;
    });
    
    // Return the template actions to the parent component
    this.dialogRef.close({
      action: 'use',
      template: template,
      actions: processedActions
    });
  }

  duplicateTemplate(template: ActionTemplate): void {
    const duplicatedTemplate: ActionTemplate = {
      ...template,
      id: this.generateId(),
      name: `${template.name} (Copy)`,
      createdAt: new Date(),
      usageCount: 0
    };
    
    this.templates.unshift(duplicatedTemplate);
    this.saveTemplates();
    this.filterTemplates();
    this.snackBar.open('Template duplicated successfully', 'Close', { duration: 3000 });
  }

  addAction(): void {
    this.actions.push(this.fb.group({
      actionType: ['', Validators.required],
      selector: [''],
      repeat: [1, [Validators.min(1)]],
      delayMs: [null, [Validators.min(0)]],
      value: ['']
    }));
  }

  removeAction(index: number): void {
    this.actions.removeAt(index);
  }

  populateFormWithActions(actions: BrowserAutomationAction[]): void {
    this.actions.clear();
    actions.forEach(action => {
      this.actions.push(this.fb.group({
        actionType: [action.actionType, Validators.required],
        selector: [action.selector || ''],
        repeat: [action.repeat || 1, [Validators.min(1)]],
        delayMs: [action.delayMs || null, [Validators.min(0)]],
        value: [action.value || '']
      }));
    });
  }

  getActionIcon(actionType: string): string {
    const action = this.actionTypes.find(a => a.value === actionType);
    return action?.icon || 'play_arrow';
  }

  getCategoryIcon(category: string): string {
    const cat = this.categories.find(c => c.value === category);
    return cat?.icon || 'folder';
  }

  getCategoryLabel(category: string): string {
    const cat = this.categories.find(c => c.value === category);
    return cat?.label || category;
  }

  actionNeedsSelector(actionType: string): boolean {
    return [
      'click', 'doubleclick', 'rightclick', 'type', 'clear', 'setValue', 
      'focus', 'blur', 'hover', 'waitForSelector', 'selectOption', 'upload',
      'drag', 'switchframe', 'switchiframe'
    ].includes(actionType);
  }

  actionNeedsValue(actionType: string): boolean {
    return [
      'navigate', 'goto', 'url', 'type', 'setValue', 'press', 'upload', 
      'wait', 'waitForTimeout', 'evaluate', 'selectOption', 'drag',
      'waitForLoadState', 'newtab', 'newpage', 'switchwindow', 'switchtab',
      'setcookies', 'addstylesheet', 'addscript', 'emulatedevice'
    ].includes(actionType);
  }

  getValueLabel(actionType: string): string {
    switch (actionType?.toLowerCase()) {
      case 'navigate':
      case 'goto':
      case 'url':
      case 'newtab':
      case 'newpage': return 'URL';
      case 'type':
      case 'setvalue': return 'Text';
      case 'wait':
      case 'waitfortimeout': return 'Duration (ms)';
      case 'press': return 'Key';
      case 'upload': return 'File Path';
      case 'evaluate':
      case 'addscript': return 'JavaScript Code';
      case 'selectoption': return 'Option Value';
      case 'drag': return 'Target Selector';
      case 'waitforloadstate': return 'Load State';
      case 'switchwindow':
      case 'switchtab': return 'Tab Index';
      case 'setcookies': return 'Cookie Data (JSON)';
      case 'addstylesheet': return 'CSS Code';
      case 'emulatedevice': return 'Device Settings (JSON)';
      default: return 'Value';
    }
  }

  getValuePlaceholder(actionType: string): string {
    switch (actionType?.toLowerCase()) {
      case 'navigate':
      case 'goto':
      case 'url':
      case 'newtab':
      case 'newpage': return 'https://example.com';
      case 'type':
      case 'setvalue': return 'Enter text to type';
      case 'wait':
      case 'waitfortimeout': return '3000';
      case 'press': return 'Enter, Space, ArrowDown, etc.';
      case 'upload': return '/path/to/file.pdf';
      case 'evaluate':
      case 'addscript': return 'console.log("Hello World");';
      case 'selectoption': return 'option1';
      case 'drag': return '#target-element';
      case 'waitforloadstate': return 'networkidle, domcontentloaded, load';
      case 'switchwindow':
      case 'switchtab': return '0, 1, 2...';
      case 'setcookies': return '{"name": "value", "domain": ".example.com"}';
      case 'addstylesheet': return '.my-class { color: red; }';
      case 'emulatedevice': return '{"width": 375, "height": 667}';
      default: return 'Enter value';
    }
  }

  getValueHint(actionType: string): string {
    switch (actionType?.toLowerCase()) {
      case 'wait':
      case 'waitfortimeout': return 'Time to wait in milliseconds';
      case 'press': return 'Valid key names from Playwright documentation';
      case 'waitforloadstate': return 'Valid states: load, domcontentloaded, networkidle';
      case 'switchwindow':
      case 'switchtab': return 'Zero-based tab index (0 = first tab)';
      case 'setcookies': return 'JSON object with cookie properties';
      case 'emulatedevice': return 'JSON with device viewport and user agent settings';
      case 'drag': return 'CSS selector of the drop target element';
      default: return '';
    }
  }

  private generateId(): string {
    return Date.now().toString(36) + Math.random().toString(36).substr(2);
  }

  close(): void {
    this.dialogRef.close({ action: 'cancel' });
  }
} 