import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AiAssistantComponent, AiAssistantConfig, AiAssistantResult } from '../../shared/components/ai-assistant/ai-assistant.component';

@Component({
  selector: 'app-ai-demo',
  templateUrl: './ai-demo.component.html',
  styleUrls: ['./ai-demo.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatTabsModule,
    MatIconModule,
    AiAssistantComponent
  ]
})
export class AiDemoComponent {
  // Configuration for general purpose AI
  generalConfig: AiAssistantConfig = {
    title: 'General AI Assistant',
    placeholder: 'Ask me anything...',
    systemPrompt: 'You are a helpful and knowledgeable assistant. Provide clear, accurate, and helpful responses.',
    showJsonSchema: true,
    showAdvancedOptions: true,
    allowConfigurationSelection: true,
    maxInputLength: 5000,
    buttonText: 'Generate Response'
  };

  // Configuration for code generation
  codeConfig: AiAssistantConfig = {
    title: 'Code Assistant',
    placeholder: 'Describe the code you want to generate...',
    systemPrompt: 'You are an expert software developer. Generate clean, well-documented, and efficient code based on the user\'s requirements. Include comments explaining the code.',
    defaultJsonSchema: JSON.stringify({
      "type": "object",
      "properties": {
        "code": {
          "type": "string",
          "description": "The generated code"
        },
        "language": {
          "type": "string", 
          "description": "Programming language used"
        },
        "explanation": {
          "type": "string",
          "description": "Brief explanation of the code"
        },
        "dependencies": {
          "type": "array",
          "items": {"type": "string"},
          "description": "Required dependencies or imports"
        }
      },
      "required": ["code", "language", "explanation"]
    }, null, 2),
    showJsonSchema: true,
    showAdvancedOptions: false,
    allowConfigurationSelection: false,
    maxInputLength: 2000,
    buttonText: 'Generate Code'
  };

  // Configuration for data analysis
  analysisConfig: AiAssistantConfig = {
    title: 'Data Analysis Assistant',
    placeholder: 'Provide data or describe your analysis needs...',
    systemPrompt: 'You are a data analyst expert. Analyze the provided data and give insights, patterns, and actionable recommendations.',
    defaultJsonSchema: JSON.stringify({
      "type": "object",
      "properties": {
        "summary": {
          "type": "string",
          "description": "Summary of the analysis"
        },
        "insights": {
          "type": "array",
          "items": {"type": "string"},
          "description": "Key insights discovered"
        },
        "recommendations": {
          "type": "array", 
          "items": {"type": "string"},
          "description": "Actionable recommendations"
        },
        "confidence": {
          "type": "number",
          "minimum": 0,
          "maximum": 100,
          "description": "Confidence level in the analysis (0-100)"
        }
      },
      "required": ["summary", "insights", "recommendations", "confidence"]
    }, null, 2),
    showJsonSchema: false,
    showAdvancedOptions: true,
    allowConfigurationSelection: true,
    maxInputLength: 8000,
    buttonText: 'Analyze Data'
  };

  // Configuration for content writing
  writingConfig: AiAssistantConfig = {
    title: 'Content Writing Assistant',
    placeholder: 'Describe the content you want to create...',
    systemPrompt: 'You are a professional content writer. Create engaging, well-structured, and compelling content based on the user\'s requirements.',
    showJsonSchema: false,
    showAdvancedOptions: true,
    allowConfigurationSelection: false,
    maxInputLength: 3000,
    buttonText: 'Create Content'
  };

  lastResults: { [key: string]: AiAssistantResult | null } = {
    general: null,
    code: null,
    analysis: null,
    writing: null
  };

  constructor(private snackBar: MatSnackBar) {}

  onResult(type: string, result: AiAssistantResult): void {
    this.lastResults[type] = result;
    console.log(`${type} result:`, result);
    
    if (result.success) {
      this.snackBar.open(`${type} response generated successfully!`, 'Close', { 
        duration: 3000,
        panelClass: 'success-snackbar'
      });
    }
  }

  onLoading(type: string, loading: boolean): void {
    console.log(`${type} loading:`, loading);
  }

  getResultsCount(): number {
    return Object.values(this.lastResults).filter(result => result !== null).length;
  }

  getSuccessfulResults(): number {
    return Object.values(this.lastResults).filter(result => result?.success).length;
  }

  getTotalTokensUsed(): number {
    return Object.values(this.lastResults)
      .filter(result => result?.success)
      .reduce((total, result) => total + (result?.tokensUsed || 0), 0);
  }
} 