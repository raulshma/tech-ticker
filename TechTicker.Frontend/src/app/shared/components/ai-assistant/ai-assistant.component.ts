import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSliderModule } from '@angular/material/slider';
import { MatSelectModule } from '@angular/material/select';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar } from '@angular/material/snack-bar';
import { 
  TechTickerApiClient, 
  GenericAiRequestDto, 
  GenericAiResponseDto, 
  AiConfigurationDto 
} from '../../api/api-client';

export interface AiAssistantConfig {
  title?: string;
  placeholder?: string;
  systemPrompt?: string;
  defaultJsonSchema?: string;
  showJsonSchema?: boolean;
  showAdvancedOptions?: boolean;
  allowConfigurationSelection?: boolean;
  maxInputLength?: number;
  buttonText?: string;
}

export interface AiAssistantResult {
  response: string;
  tokensUsed: number;
  model: string;
  isStructuredOutput: boolean;
  generatedAt: Date;
  success: boolean;
  errorMessage?: string;
}

@Component({
  selector: 'app-ai-assistant',
  templateUrl: './ai-assistant.component.html',
  styleUrls: ['./ai-assistant.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSliderModule,
    MatSelectModule,
    MatExpansionModule,
    MatTooltipModule
  ]
})
export class AiAssistantComponent implements OnInit {
  @Input() config: AiAssistantConfig = {};
  @Input() disabled = false;
  @Output() result = new EventEmitter<AiAssistantResult>();
  @Output() loading = new EventEmitter<boolean>();

  aiForm: FormGroup;
  isLoading = false;
  configurations: AiConfigurationDto[] = [];
  lastResult: AiAssistantResult | null = null;

  // Default configuration
  defaultConfig: AiAssistantConfig = {
    title: 'AI Assistant',
    placeholder: 'Enter your text here...',
    showJsonSchema: true,
    showAdvancedOptions: true,
    allowConfigurationSelection: true,
    maxInputLength: 10000,
    buttonText: 'Generate Response'
  };

  constructor(
    private fb: FormBuilder,
    private apiClient: TechTickerApiClient,
    private snackBar: MatSnackBar
  ) {
    this.aiForm = this.createForm();
  }

  ngOnInit(): void {
    // Merge default config with provided config
    this.config = { ...this.defaultConfig, ...this.config };
    
    // Update form with config values
    this.updateFormWithConfig();
    
    // Load AI configurations if needed
    if (this.config.allowConfigurationSelection) {
      this.loadConfigurations();
    }
  }

  private createForm(): FormGroup {
    return this.fb.group({
      inputText: ['', [Validators.required, Validators.minLength(1)]],
      systemPrompt: [''],
      context: [''],
      jsonSchema: [''],
      aiConfigurationId: [''],
      temperature: [0.7, [Validators.min(0), Validators.max(2)]],
      maxTokens: [null, [Validators.min(1), Validators.max(8192)]]
    });
  }

  private updateFormWithConfig(): void {
    if (this.config.systemPrompt) {
      this.aiForm.patchValue({ systemPrompt: this.config.systemPrompt });
    }
    if (this.config.defaultJsonSchema) {
      this.aiForm.patchValue({ jsonSchema: this.config.defaultJsonSchema });
    }
    
    // Update validators based on config
    const inputTextControl = this.aiForm.get('inputText');
    if (inputTextControl && this.config.maxInputLength) {
      inputTextControl.setValidators([
        Validators.required,
        Validators.minLength(1),
        Validators.maxLength(this.config.maxInputLength)
      ]);
      inputTextControl.updateValueAndValidity();
    }
  }

  private async loadConfigurations(): Promise<void> {
    try {
      const result = await this.apiClient.getActiveConfigurations().toPromise();
      this.configurations = result?.data || [];
    } catch (error) {
      console.error('Error loading AI configurations:', error);
    }
  }

  async generateResponse(): Promise<void> {
    if (this.aiForm.invalid || this.disabled) {
      return;
    }

    this.isLoading = true;
    this.loading.emit(true);

    try {
      const formValue = this.aiForm.value;
      
      const request = new GenericAiRequestDto({
        inputText: formValue.inputText,
        systemPrompt: formValue.systemPrompt || this.config.systemPrompt,
        context: formValue.context,
        jsonSchema: formValue.jsonSchema || this.config.defaultJsonSchema,
        aiConfigurationId: formValue.aiConfigurationId || undefined,
        temperature: formValue.temperature,
        maxTokens: formValue.maxTokens
      });

      const response = await this.apiClient.generateGenericResponse(request).toPromise();
      
      if (response?.success && response.data) {
        const result: AiAssistantResult = {
          response: response.data.response!,
          tokensUsed: response.data.tokensUsed!,
          model: response.data.model!,
          isStructuredOutput: response.data.isStructuredOutput!,
          generatedAt: response.data.generatedAt!,
          success: response.data.success!,
          errorMessage: response.data.errorMessage
        };

        this.lastResult = result;
        this.result.emit(result);
        
        if (result.success) {
          this.snackBar.open('Response generated successfully', 'Close', { duration: 3000 });
        } else {
          this.snackBar.open(`Error: ${result.errorMessage}`, 'Close', { duration: 5000 });
        }
      } else {
        const errorMessage = response?.message || 'Failed to generate response';
        this.snackBar.open(errorMessage, 'Close', { duration: 5000 });
        
        const errorResult: AiAssistantResult = {
          response: '',
          tokensUsed: 0,
          model: '',
          isStructuredOutput: false,
          generatedAt: new Date(),
          success: false,
          errorMessage: errorMessage
        };
        
        this.lastResult = errorResult;
        this.result.emit(errorResult);
      }
    } catch (error: any) {
      console.error('Error generating AI response:', error);
      const errorMessage = error?.message || 'An unexpected error occurred';
      this.snackBar.open(errorMessage, 'Close', { duration: 5000 });
      
      const errorResult: AiAssistantResult = {
        response: '',
        tokensUsed: 0,
        model: '',
        isStructuredOutput: false,
        generatedAt: new Date(),
        success: false,
        errorMessage: errorMessage
      };
      
      this.lastResult = errorResult;
      this.result.emit(errorResult);
    } finally {
      this.isLoading = false;
      this.loading.emit(false);
    }
  }

  clearForm(): void {
    this.aiForm.reset({
      temperature: 0.7,
      systemPrompt: this.config.systemPrompt || '',
      jsonSchema: this.config.defaultJsonSchema || ''
    });
    this.lastResult = null;
  }

  copyResponse(): void {
    if (this.lastResult?.response) {
      navigator.clipboard.writeText(this.lastResult.response).then(() => {
        this.snackBar.open('Response copied to clipboard', 'Close', { duration: 2000 });
      }).catch(() => {
        this.snackBar.open('Failed to copy to clipboard', 'Close', { duration: 3000 });
      });
    }
  }

  formatJsonSchema(): void {
    const jsonSchemaControl = this.aiForm.get('jsonSchema');
    if (jsonSchemaControl?.value) {
      try {
        const parsed = JSON.parse(jsonSchemaControl.value);
        const formatted = JSON.stringify(parsed, null, 2);
        jsonSchemaControl.setValue(formatted);
      } catch (error) {
        this.snackBar.open('Invalid JSON schema format', 'Close', { duration: 3000 });
      }
    }
  }

  getErrorMessage(fieldName: string): string {
    const control = this.aiForm.get(fieldName);
    if (control?.hasError('required')) {
      return `${fieldName} is required`;
    }
    if (control?.hasError('minlength')) {
      return `${fieldName} is too short`;
    }
    if (control?.hasError('maxlength')) {
      return `${fieldName} is too long`;
    }
    if (control?.hasError('min')) {
      return `${fieldName} value is too low`;
    }
    if (control?.hasError('max')) {
      return `${fieldName} value is too high`;
    }
    return '';
  }

  get effectiveConfig(): AiAssistantConfig {
    return this.config;
  }

  get showResult(): boolean {
    return this.lastResult !== null;
  }

  get isJsonResponse(): boolean {
    return this.lastResult?.isStructuredOutput || false;
  }

  get formattedResponse(): string {
    if (!this.lastResult?.response) return '';
    
    if (this.isJsonResponse) {
      try {
        const parsed = JSON.parse(this.lastResult.response);
        return JSON.stringify(parsed, null, 2);
      } catch {
        return this.lastResult.response;
      }
    }
    
    return this.lastResult.response;
  }
} 