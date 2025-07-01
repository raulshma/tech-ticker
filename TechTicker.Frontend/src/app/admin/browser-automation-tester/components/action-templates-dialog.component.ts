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

  // Action types for reference
  actionTypes = [
    { value: 'scroll', label: 'Scroll Down', icon: 'unfold_more' },
    { value: 'click', label: 'Click Element', icon: 'touch_app' },
    { value: 'waitForSelector', label: 'Wait for Selector', icon: 'schedule' },
    { value: 'type', label: 'Type Text', icon: 'keyboard' },
    { value: 'wait', label: 'Wait (Timeout)', icon: 'timer' },
    { value: 'waitForTimeout', label: 'Wait for Timeout', icon: 'timer' },
    { value: 'screenshot', label: 'Take Screenshot', icon: 'camera_alt' },
    { value: 'evaluate', label: 'Execute JavaScript', icon: 'code' },
    { value: 'hover', label: 'Hover Element', icon: 'mouse' },
    { value: 'selectOption', label: 'Select Option', icon: 'arrow_drop_down' },
    { value: 'setValue', label: 'Set Value (JS)', icon: 'input' }
  ];

  constructor(
    private fb: FormBuilder,
    private snackBar: MatSnackBar,
    public dialogRef: MatDialogRef<ActionTemplatesDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { currentActions?: BrowserAutomationAction[] }
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
    
    // Return the template actions to the parent component
    this.dialogRef.close({
      action: 'use',
      template: template,
      actions: template.actions
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
    return ['click', 'waitForSelector', 'type', 'hover', 'selectOption', 'setValue'].includes(actionType);
  }

  actionNeedsValue(actionType: string): boolean {
    return ['type', 'wait', 'waitForTimeout', 'evaluate', 'setValue'].includes(actionType);
  }

  private generateId(): string {
    return Date.now().toString(36) + Math.random().toString(36).substr(2);
  }

  close(): void {
    this.dialogRef.close({ action: 'cancel' });
  }
} 