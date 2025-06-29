import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { TechTickerApiClient, AiConfigurationDto, CreateAiConfigurationDto, UpdateAiConfigurationDto } from '../../shared/api/api-client';

@Component({
  selector: 'app-ai-settings',
  templateUrl: './ai-settings.component.html',
  styleUrls: ['./ai-settings.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatSlideToggleModule,
    MatChipsModule,
    MatProgressSpinnerModule
  ]
})
export class AiSettingsComponent implements OnInit {
  configurations: AiConfigurationDto[] = [];
  loading = false;
  editingConfiguration: AiConfigurationDto | null = null;
  showForm = false;
  
  aiForm: FormGroup;
  displayedColumns: string[] = ['name', 'provider', 'model', 'isDefault', 'isActive', 'actions'];
  
  // Available options
  providers = [
    { value: 'Google', label: 'Google Gemini' },
    { value: 'OpenAI', label: 'OpenAI' },
    { value: 'Anthropic', label: 'Anthropic Claude' }
  ];
  
  capabilities = [
    'Structured outputs',
    'Caching', 
    'Tuning',
    'Function calling',
    'Code execution',
    'Search grounding',
    'Image generation',
    'Audio generation',
    'Live API',
    'Thinking'
  ];
  
  inputTypes = ['Audio', 'Images', 'Video', 'Text', 'PDF'];
  outputTypes = ['Text', 'Images', 'Audio'];

  constructor(
    private fb: FormBuilder,
    private apiClient: TechTickerApiClient,
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) {
    this.aiForm = this.createForm();
  }

  ngOnInit(): void {
    this.loadConfigurations();
  }

  createForm(): FormGroup {
    return this.fb.group({
      provider: ['Google', Validators.required],
      name: ['', [Validators.required, Validators.maxLength(200)]],
      description: [''],
      openApiCompatibleUrl: [''],
      apiKey: ['', Validators.required],
      model: ['', Validators.required],
      inputTokenLimit: [null, [Validators.min(1)]],
      outputTokenLimit: [null, [Validators.min(1)]],
      capabilities: [[]],
      supportedInputTypes: [[]],
      supportedOutputTypes: [[]],
      rateLimitRpm: [null, [Validators.min(1)]],
      rateLimitTpm: [null, [Validators.min(1)]],
      rateLimitRpd: [null, [Validators.min(1)]],
      isActive: [true],
      isDefault: [false]
    });
  }

  async loadConfigurations(): Promise<void> {
    this.loading = true;
    try {
      const result = await this.apiClient.getAllConfigurations().toPromise();
      this.configurations = result?.data || [];
    } catch (error) {
      console.error('Error loading AI configurations:', error);
      this.snackBar.open('Failed to load AI configurations', 'Close', { duration: 3000 });
    } finally {
      this.loading = false;
    }
  }

  showAddForm(): void {
    this.editingConfiguration = null;
    this.aiForm.reset({
      provider: 'Google',
      isActive: true,
      isDefault: false,
      capabilities: [],
      supportedInputTypes: [],
      supportedOutputTypes: []
    });
    this.showForm = true;
  }

  editConfiguration(config: AiConfigurationDto): void {
    this.editingConfiguration = config;
    this.aiForm.patchValue(config);
    this.aiForm.patchValue({ apiKey: '***' }); // Don't show the actual API key
    this.showForm = true;
  }

  cancelEdit(): void {
    this.showForm = false;
    this.editingConfiguration = null;
    this.aiForm.reset();
  }

  async saveConfiguration(): Promise<void> {
    if (this.aiForm.invalid) {
      return;
    }

    this.loading = true;
    try {
      const formValue = this.aiForm.value;
      
      if (this.editingConfiguration) {
        const updateDto: UpdateAiConfigurationDto = formValue;
        if (formValue.apiKey === '***') {
          delete updateDto.apiKey; // Don't update if unchanged
        }
        await this.apiClient.updateConfiguration(this.editingConfiguration.aiConfigurationId!, updateDto).toPromise();
        this.snackBar.open('Configuration updated successfully', 'Close', { duration: 3000 });
      } else {
        const createDto: CreateAiConfigurationDto = formValue;
        await this.apiClient.createConfiguration(createDto).toPromise();
        this.snackBar.open('Configuration created successfully', 'Close', { duration: 3000 });
      }

      await this.loadConfigurations();
      this.cancelEdit();
    } catch (error) {
      console.error('Error saving configuration:', error);
      this.snackBar.open('Failed to save configuration', 'Close', { duration: 3000 });
    } finally {
      this.loading = false;
    }
  }

  async deleteConfiguration(config: AiConfigurationDto): Promise<void> {
    if (!confirm(`Are you sure you want to delete "${config.name}"?`)) {
      return;
    }

    this.loading = true;
    try {
      await this.apiClient.deleteConfiguration(config.aiConfigurationId!).toPromise();
      this.snackBar.open('Configuration deleted successfully', 'Close', { duration: 3000 });
      await this.loadConfigurations();
    } catch (error) {
      console.error('Error deleting configuration:', error);
      this.snackBar.open('Failed to delete configuration', 'Close', { duration: 3000 });
    } finally {
      this.loading = false;
    }
  }

  async setDefault(config: AiConfigurationDto): Promise<void> {
    this.loading = true;
    try {
      await this.apiClient.setDefaultConfiguration(config.aiConfigurationId!).toPromise();
      this.snackBar.open('Default configuration updated', 'Close', { duration: 3000 });
      await this.loadConfigurations();
    } catch (error) {
      console.error('Error setting default:', error);
      this.snackBar.open('Failed to set default', 'Close', { duration: 3000 });
    } finally {
      this.loading = false;
    }
  }

  async testConfiguration(config: AiConfigurationDto): Promise<void> {
    this.loading = true;
    try {
      const result = await this.apiClient.testConfiguration(config.aiConfigurationId!).toPromise();
      const message = result?.data ? 'Test successful' : 'Test failed';
      this.snackBar.open(message, 'Close', { duration: 3000 });
    } catch (error) {
      this.snackBar.open('Test failed', 'Close', { duration: 3000 });
    } finally {
      this.loading = false;
    }
  }

  getErrorMessage(fieldName: string): string {
    const control = this.aiForm.get(fieldName);
    if (control?.errors && control.touched) {
      if (control.errors['required']) return `${fieldName} is required`;
      if (control.errors['maxlength']) return `${fieldName} is too long`;
      if (control.errors['min']) return `${fieldName} must be greater than 0`;
    }
    return '';
  }
} 