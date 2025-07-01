import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTabsModule } from '@angular/material/tabs';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatBadgeModule } from '@angular/material/badge';

export interface ProductSpecification {
  isSuccess: boolean;
  errorMessage?: string;
  specifications: { [key: string]: any };
  typedSpecifications: { [key: string]: TypedSpecification };
  categorizedSpecs: { [key: string]: CategoryGroup };
  metadata: ParseMetadata;
  quality: QualityMetrics;
  parsingTimeMs: number;
}

export interface TypedSpecification {
  value: any;
  type: string;
  unit: string;
  numericValue?: number;
  confidence: number;
  category: string;
  hasMultipleValues: boolean;
  valueCount: number;
  alternatives: SpecificationValue[];
}

export interface CategoryGroup {
  name: string;
  specifications: { [key: string]: TypedSpecification };
  order: number;
  confidence: number;
  isExplicit: boolean;
  itemCount: number;
  multiValueCount: number;
}

export interface ParseMetadata {
  totalRows: number;
  dataRows: number;
  headerRows: number;
  continuationRows: number;
  inlineValueCount: number;
  multiValueSpecs: number;
  structure: string;
  warnings: string[];
  processingTimeMs: number;
}

export interface QualityMetrics {
  overallScore: number;
  structureConfidence: number;
  typeDetectionAccuracy: number;
  completenessScore: number;
}

export interface SpecificationValue {
  value: string;
  normalizedValue: string;
  numericValue?: number;
  unit: string;
  type: string;
  confidence: number;
  isListItem: boolean;
  isContinuation: boolean;
  isInlineValue: boolean;
  order: number;
  prefix: string;
}

@Component({
  selector: 'app-product-specifications',
  standalone: true,
  imports: [
    CommonModule,
    MatTabsModule,
    MatIconModule,
    MatCardModule,
    MatBadgeModule
  ],
  templateUrl: './product-specifications.component.html',
  styleUrls: ['./product-specifications.component.scss']
})
export class ProductSpecificationsComponent {
  @Input() specifications?: ProductSpecification;
  @Input() showMetadata = false;

  activeTab = 'categorized';
  
  get activeTabIndex(): number {
    switch (this.activeTab) {
      case 'categorized': return 0;
      case 'flat': return 1;
      case 'metadata': return 2;
      default: return 0;
    }
  }
  
  get hasSpecifications(): boolean {
    return (this.specifications?.isSuccess ?? false) && 
           Object.keys(this.specifications?.specifications ?? {}).length > 0;
  }

  get categoryEntries(): [string, CategoryGroup][] {
    if (!this.specifications?.categorizedSpecs) return [];
    return Object.entries(this.specifications.categorizedSpecs)
      .sort(([,a], [,b]) => a.order - b.order);
  }

  get flatSpecifications(): [string, TypedSpecification][] {
    if (!this.specifications?.typedSpecifications) return [];
    return Object.entries(this.specifications.typedSpecifications);
  }

  objectEntries(obj: any): [string, any][] {
    return Object.entries(obj || {});
  }

  formatValue(spec: TypedSpecification): string {
    if (spec.hasMultipleValues && Array.isArray(spec.value)) {
      return spec.value.join(', ');
    }
    
    if (typeof spec.value === 'object' && spec.value !== null) {
      return JSON.stringify(spec.value);
    }
    
    return String(spec.value || '');
  }

  getTypeIcon(type: string): string {
    const icons: { [key: string]: string } = {
      'Text': 'text_fields',
      'Numeric': 'tag',
      'Memory': 'memory',
      'Clock': 'schedule',
      'Power': 'power',
      'Dimension': 'straighten',
      'Interface': 'cable',
      'Resolution': 'display_settings',
      'List': 'list'
    };
    
    return icons[type] || 'info';
  }

  getConfidenceColor(confidence: number): string {
    if (confidence >= 0.9) return 'success';
    if (confidence >= 0.7) return 'warning';
    return 'danger';
  }
} 