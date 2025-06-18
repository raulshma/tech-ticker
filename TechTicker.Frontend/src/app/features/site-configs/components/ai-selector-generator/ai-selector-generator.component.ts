import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
  SiteConfigurationService,
  SelectorGenerationResult,
  SelectorTestResult,
  SelectorSuggestion
} from '../../services/site-configuration.service';
import {
  SelectorSet,
  FieldTestResult,
  GenerateSelectorsRequest,
  TestSelectorsRequest,
  SelectorImprovementRequest,
  SaveSiteConfigurationRequest
} from '../../../../shared/api/api-client';

@Component({
  selector: 'app-ai-selector-generator',
  templateUrl: './ai-selector-generator.component.html',
  styleUrls: ['./ai-selector-generator.component.scss'],
  standalone: false
})
export class AiSelectorGeneratorComponent {
  generatorForm: FormGroup;
  testForm: FormGroup;
  isGenerating = false;
  isTesting = false;

  generationResult: SelectorGenerationResult | null = null;
  testResult: SelectorTestResult | null = null;
  suggestions: SelectorSuggestion[] = [];

  constructor(
    private formBuilder: FormBuilder,
    private siteConfigurationService: SiteConfigurationService,
    private snackBar: MatSnackBar
  ) {
    this.generatorForm = this.formBuilder.group({
      domain: ['', [Validators.required]],
      siteName: [''],
      htmlContent: ['', [Validators.required]],
      notes: ['']
    });

    this.testForm = this.formBuilder.group({
      testHtmlContent: ['', [Validators.required]]
    });
  }

  generateSelectors(): void {
    if (this.generatorForm.valid && !this.isGenerating) {
      this.isGenerating = true;
      this.generationResult = null;

      const request = new GenerateSelectorsRequest({
        domain: this.generatorForm.value.domain,
        siteName: this.generatorForm.value.siteName,
        htmlContent: this.generatorForm.value.htmlContent,
        notes: this.generatorForm.value.notes
      });

      this.siteConfigurationService.generateSelectors(request).subscribe({
        next: (result) => {
          this.generationResult = result;
          this.snackBar.open('Selectors generated successfully!', 'Close', { duration: 3000 });
          this.isGenerating = false;
        },
        error: (error) => {
          console.error('Error generating selectors:', error);
          this.snackBar.open('Failed to generate selectors. Please try again.', 'Close', { duration: 5000 });
          this.isGenerating = false;
        }
      });
    }
  }

  testSelectors(): void {
    if (this.testForm.valid && this.generationResult && !this.isTesting) {
      this.isTesting = true;
      this.testResult = null;

      const request = new TestSelectorsRequest({
        htmlContent: this.testForm.value.testHtmlContent,
        selectors: this.generationResult.selectors!
      });

      this.siteConfigurationService.testSelectors(request).subscribe({
        next: (result) => {
          this.testResult = result;
          this.snackBar.open('Selectors tested successfully!', 'Close', { duration: 3000 });
          this.isTesting = false;
        },
        error: (error) => {
          console.error('Error testing selectors:', error);
          this.snackBar.open('Failed to test selectors. Please try again.', 'Close', { duration: 5000 });
          this.isTesting = false;
        }
      });
    }
  }

  getSuggestions(): void {
    if (this.generationResult && this.testResult) {
      const request = new SelectorImprovementRequest({
        htmlContent: this.testForm.value.testHtmlContent,
        currentSelectors: this.generationResult.selectors!,
        testResult: this.testResult
      });

      this.siteConfigurationService.suggestImprovements(request).subscribe({
        next: (suggestions) => {
          this.suggestions = suggestions;
          this.snackBar.open('Improvement suggestions generated!', 'Close', { duration: 3000 });
        },
        error: (error) => {
          console.error('Error getting suggestions:', error);
          this.snackBar.open('Failed to get suggestions. Please try again.', 'Close', { duration: 5000 });
        }
      });
    }
  }

  saveConfiguration(): void {
    if (this.generationResult) {
      const saveRequest = new SaveSiteConfigurationRequest({
        domain: this.generationResult.domain!,
        siteName: this.generatorForm.value.siteName || this.generationResult.domain,
        selectors: this.generationResult.selectors!,
        isActive: true,
        notes: this.generationResult.notes,
        testHtml: this.generatorForm.value.htmlContent
      });

      this.siteConfigurationService.createConfiguration(saveRequest).subscribe({
        next: (config) => {
          this.snackBar.open('Site configuration saved successfully!', 'Close', { duration: 3000 });
          // Reset form or navigate away
          this.resetForm();
        },
        error: (error) => {
          console.error('Error saving configuration:', error);
          this.snackBar.open('Failed to save configuration. Please try again.', 'Close', { duration: 5000 });
        }
      });
    }
  }

  resetForm(): void {
    this.generatorForm.reset();
    this.testForm.reset();
    this.generationResult = null;
    this.testResult = null;
    this.suggestions = [];
  }

  getSelectorsArray(selectors: string[] | undefined): string[] {
    return selectors || [];
  }

  getConfidenceColor(confidence: number): string {
    if (confidence >= 0.8) return 'primary';
    if (confidence >= 0.6) return 'accent';
    return 'warn';
  }

  getScoreColor(score: number): string {
    if (score >= 0.8) return 'primary';
    if (score >= 0.6) return 'accent';
    return 'warn';
  }

  getPriorityColor(priority: string): string {
    switch (priority.toLowerCase()) {
      case 'high': return 'warn';
      case 'medium': return 'accent';
      case 'low': return 'primary';
      default: return 'primary';
    }
  }

  getTestResultEntries(): Array<{key: string, value: FieldTestResult}> {
    if (!this.testResult?.results) {
      return [];
    }
    return Object.entries(this.testResult.results).map(([key, value]) => ({key, value}));
  }
}
