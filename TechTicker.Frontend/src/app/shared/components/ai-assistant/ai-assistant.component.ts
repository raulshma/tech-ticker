import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSliderModule } from '@angular/material/slider';
import { MatSelectModule } from '@angular/material/select';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { MatSnackBar } from '@angular/material/snack-bar';
import { 
  TechTickerApiClient, 
  GenericAiRequestDto, 
  GenericAiResponseDto, 
  AiConfigurationDto 
} from '../../api/api-client';

export interface AiAssistantConfig {
  title?: string;
  subtitle?: string;
  icon?: string;
  inputLabel?: string;
  placeholder?: string;
  defaultSystemPrompt?: string;
  defaultJsonSchema?: string;
  allowJsonSchema?: boolean;
  showAdvancedOptions?: boolean;
  allowSystemPrompt?: boolean;
  allowContext?: boolean;
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
    MatProgressBarModule,
    MatSliderModule,
    MatSelectModule,
    MatExpansionModule,
    MatTooltipModule,
    MatChipsModule
  ]
})
export class AiAssistantComponent implements OnInit {
  @Input() config: AiAssistantConfig = {};
  @Input() disabled = false;
  @Output() result = new EventEmitter<AiAssistantResult>();
  @Output() loading = new EventEmitter<boolean>();

  assistantForm: FormGroup;
  isLoading = false;
  aiConfigurations: AiConfigurationDto[] = [];
  lastResult: AiAssistantResult | null = null;

  // Default configuration
  defaultConfig: AiAssistantConfig = {
    title: 'AI Assistant',
    subtitle: 'Get AI-powered assistance with your tasks',
    icon: 'smart_toy',
    inputLabel: 'Enter your request',
    placeholder: 'Type your message here...',
    allowJsonSchema: true,
    showAdvancedOptions: true,
    allowSystemPrompt: true,
    allowContext: true,
    allowConfigurationSelection: true,
    maxInputLength: 5000,
    buttonText: 'Generate Response'
  };

  constructor(
    private fb: FormBuilder,
    private apiClient: TechTickerApiClient,
    private snackBar: MatSnackBar
  ) {
    this.assistantForm = this.createForm();
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
      inputText: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(5000)]],
      systemPrompt: ['', [Validators.maxLength(1000)]],
      context: ['', [Validators.maxLength(2000)]],
      jsonSchema: ['', [this.jsonSchemaValidator]],
      aiConfigurationId: [null],
      temperature: [0.7, [Validators.min(0), Validators.max(1)]],
      maxTokens: [1000, [Validators.min(100), Validators.max(4000)]]
    });
  }

  private jsonSchemaValidator(control: AbstractControl): { [key: string]: any } | null {
    if (!control.value) {
      return null; // Allow empty values
    }
    
    try {
      JSON.parse(control.value);
      return null;
    } catch (error) {
      return { invalidJson: true };
    }
  }

  private updateFormWithConfig(): void {
    if (this.config.defaultSystemPrompt) {
      this.assistantForm.patchValue({ systemPrompt: this.config.defaultSystemPrompt });
    }
    if (this.config.defaultJsonSchema) {
      this.assistantForm.patchValue({ jsonSchema: this.config.defaultJsonSchema });
    }
    
    // Update validators based on config
    const inputTextControl = this.assistantForm.get('inputText');
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
      this.aiConfigurations = result?.data || [];
    } catch (error) {
      console.error('Error loading AI configurations:', error);
    }
  }

  async onSubmit(): Promise<void> {
    if (this.assistantForm.invalid || this.disabled) {
      return;
    }

    this.isLoading = true;
    this.loading.emit(true);

    try {
      const formValue = this.assistantForm.value;
      
      const request = new GenericAiRequestDto({
        inputText: formValue.inputText,
        systemPrompt: formValue.systemPrompt || this.config.defaultSystemPrompt,
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
      this.snackBar.open(`Error: ${errorMessage}`, 'Close', { duration: 5000 });
      
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
    this.assistantForm.reset({
      temperature: 0.7,
      maxTokens: 1000
    });
    this.lastResult = null;
    
    // Reapply config defaults
    this.updateFormWithConfig();
  }

  copyResponse(): void {
    if (this.lastResult?.response) {
      navigator.clipboard.writeText(this.lastResult.response).then(() => {
        this.snackBar.open('Response copied to clipboard', 'Close', { duration: 2000 });
      }).catch(err => {
        console.error('Failed to copy response:', err);
        this.snackBar.open('Failed to copy response', 'Close', { duration: 2000 });
      });
    }
  }

  getJsonSchemaPlaceholder(): string {
    return `{
  "type": "object",
  "properties": {
    "result": {
      "type": "string",
      "description": "The main response"
    },
    "confidence": {
      "type": "number",
      "description": "Confidence level (0-1)"
    }
  },
  "required": ["result"]
}`;
  }

  formatJsonResponse(response: string): string {
    try {
      const parsed = JSON.parse(response);
      return JSON.stringify(parsed, null, 2);
    } catch (error) {
      return response;
    }
  }

  formatNaturalResponse(response: string): string {
    // Convert markdown-like formatting to HTML
    return response
      .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
      .replace(/\*(.*?)\*/g, '<em>$1</em>')
      .replace(/`(.*?)`/g, '<code>$1</code>')
      .replace(/\n\n/g, '</p><p>')
      .replace(/\n/g, '<br>')
      .replace(/^(.*)$/, '<p>$1</p>');
  }

  getErrorMessage(fieldName: string): string {
    const control = this.assistantForm.get(fieldName);
    if (control?.errors) {
      if (control.errors['required']) {
        return `${fieldName} is required`;
      }
      if (control.errors['minlength']) {
        return `${fieldName} is too short`;
      }
      if (control.errors['maxlength']) {
        return `${fieldName} is too long`;
      }
      if (control.errors['min']) {
        return `${fieldName} value is too low`;
      }
      if (control.errors['max']) {
        return `${fieldName} value is too high`;
      }
      if (control.errors['invalidJson']) {
        return 'Invalid JSON format';
      }
    }
    return '';
  }
} 