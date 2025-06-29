import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatExpansionModule } from '@angular/material/expansion';
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
    MatChipsModule,
    MatExpansionModule,
    AiAssistantComponent
  ]
})
export class AiDemoComponent {
  selectedTabIndex = 0;
  totalRequests = 0;
  successfulRequests = 0;

  // Configuration for general purpose AI
  generalConfig: AiAssistantConfig = {
    title: 'General Purpose AI',
    subtitle: 'Versatile assistant with full customization',
    icon: 'auto_awesome',
    inputLabel: 'Enter your request',
    placeholder: 'Ask me anything - from creative writing to problem solving...',
    defaultSystemPrompt: 'You are a helpful and knowledgeable assistant. Provide clear, accurate, and helpful responses to any question or request.',
    allowJsonSchema: true,
    showAdvancedOptions: true,
    allowSystemPrompt: true,
    allowContext: true,
    allowConfigurationSelection: true,
    maxInputLength: 5000,
    buttonText: 'Generate Response'
  };

  // Configuration for code generation
  codeConfig: AiAssistantConfig = {
    title: 'Code Assistant',
    subtitle: 'Specialized for programming tasks',
    icon: 'code',
    inputLabel: 'Describe your coding needs',
    placeholder: 'Describe the code you want to generate, debug, or optimize...',
    defaultSystemPrompt: 'You are an expert software developer. Generate clean, well-documented, and efficient code based on the user\'s requirements. Always include comments explaining the code and best practices.',
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
          "description": "Detailed explanation of the code and its functionality"
        },
        "dependencies": {
          "type": "array",
          "items": {"type": "string"},
          "description": "Required dependencies, imports, or packages"
        },
        "usage": {
          "type": "string",
          "description": "How to use or run the code"
        }
      },
      "required": ["code", "language", "explanation"]
    }, null, 2),
    allowJsonSchema: true,
    showAdvancedOptions: true,
    allowSystemPrompt: false,
    allowContext: true,
    allowConfigurationSelection: false,
    maxInputLength: 3000,
    buttonText: 'Generate Code'
  };

  // Configuration for data analysis
  dataConfig: AiAssistantConfig = {
    title: 'Data Analysis Assistant',
    subtitle: 'Insights and recommendations from your data',
    icon: 'analytics',
    inputLabel: 'Provide your data or analysis request',
    placeholder: 'Share your data, metrics, or describe what you want to analyze...',
    defaultSystemPrompt: 'You are a senior data analyst and business intelligence expert. Analyze the provided data thoroughly and provide actionable insights, trends, and strategic recommendations.',
    defaultJsonSchema: JSON.stringify({
      "type": "object",
      "properties": {
        "summary": {
          "type": "string",
          "description": "Executive summary of the analysis"
        },
        "keyMetrics": {
          "type": "object",
          "description": "Important metrics and KPIs identified"
        },
        "insights": {
          "type": "array",
          "items": {"type": "string"},
          "description": "Key insights and patterns discovered"
        },
        "recommendations": {
          "type": "array", 
          "items": {
            "type": "object",
            "properties": {
              "action": {"type": "string"},
              "priority": {"type": "string", "enum": ["High", "Medium", "Low"]},
              "impact": {"type": "string"}
            }
          },
          "description": "Actionable recommendations with priority"
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
    allowJsonSchema: true,
    showAdvancedOptions: true,
    allowSystemPrompt: true,
    allowContext: true,
    allowConfigurationSelection: true,
    maxInputLength: 8000,
    buttonText: 'Analyze Data'
  };

  // Configuration for content writing
  contentConfig: AiAssistantConfig = {
    title: 'Content Writing Assistant',
    subtitle: 'Creative and professional content generation',
    icon: 'edit',
    inputLabel: 'Describe your content needs',
    placeholder: 'Describe the content you want to create - articles, marketing copy, stories...',
    defaultSystemPrompt: 'You are a professional content writer and copywriter with expertise in various writing styles. Create engaging, well-structured, and compelling content that resonates with the target audience.',
    allowJsonSchema: false,
    showAdvancedOptions: true,
    allowSystemPrompt: true,
    allowContext: true,
    allowConfigurationSelection: false,
    maxInputLength: 4000,
    buttonText: 'Create Content'
  };

  lastResults: { [key: string]: AiAssistantResult | null } = {
    general: null,
    code: null,
    data: null,
    content: null
  };

  constructor(private snackBar: MatSnackBar) {}

  onResult(result: AiAssistantResult, type: string): void {
    this.lastResults[type] = result;
    this.totalRequests++;
    
    if (result.success) {
      this.successfulRequests++;
    }
    
    console.log(`${type} result:`, result);
    
    if (result.success) {
      this.snackBar.open(`${this.getTypeDisplayName(type)} response generated successfully!`, 'Close', { 
        duration: 3000,
        panelClass: 'success-snackbar'
      });
    } else {
      this.snackBar.open(`Failed to generate ${this.getTypeDisplayName(type)} response`, 'Close', { 
        duration: 4000,
        panelClass: 'error-snackbar'
      });
    }
  }

  onLoading(loading: boolean, type: string): void {
    console.log(`${type} loading:`, loading);
  }

  onTabChange(event: any): void {
    this.selectedTabIndex = event.index;
    console.log('Tab changed to:', event.index);
  }

  getSuccessRate(): number {
    if (this.totalRequests === 0) return 0;
    return Math.round((this.successfulRequests / this.totalRequests) * 100);
  }

  private getTypeDisplayName(type: string): string {
    const typeMap: { [key: string]: string } = {
      general: 'General Purpose',
      code: 'Code Assistant',
      data: 'Data Analysis',
      content: 'Content Writing'
    };
    return typeMap[type] || type;
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