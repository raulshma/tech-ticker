import { Component, Input, OnInit } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import {
  SpecificationComparisonDto,
  SpecificationDifferenceDto,
  SpecificationMatchDto,
  ProductWithCurrentPricesDto,
  ComparisonResultType,
  CategoryScoreDto
} from '../../services/product-comparison.service';

interface SpecificationDisplayDto {
  key: string;
  displayName: string;
  category: string;
  product1Value: string;
  product2Value: string;
  comparisonResult: ComparisonResultType;
  impactScore?: number;
  analysisNote?: string;
  isMatch: boolean;
}

// Enum mapping for ComparisonResultType since the generated enum uses numeric values
const ComparisonResultMapping = {
  EQUIVALENT: 0,           // ComparisonResultType._0
  PRODUCT1_BETTER: 1,      // ComparisonResultType._1
  PRODUCT2_BETTER: 2,      // ComparisonResultType._2
  PRODUCT1_ONLY: 3,        // ComparisonResultType._3
  PRODUCT2_ONLY: 4,        // ComparisonResultType._4
  INCOMPARABLE: 5          // ComparisonResultType._5
} as const;

@Component({
  selector: 'app-specification-comparison',
  templateUrl: './specification-comparison.component.html',
  styleUrls: ['./specification-comparison.component.scss'],
  standalone: false
})
export class SpecificationComparisonComponent implements OnInit {
  @Input() comparison?: SpecificationComparisonDto;
  @Input() product1?: ProductWithCurrentPricesDto;
  @Input() product2?: ProductWithCurrentPricesDto;

  displayedColumns: string[] = ['specification', 'product1', 'product2', 'comparison', 'impact'];
  dataSource = new MatTableDataSource<SpecificationDisplayDto>();

  categories: string[] = [];
  selectedCategory: string = 'all';
  categoryScores: { [key: string]: CategoryScoreDto } = {};

  ngOnInit(): void {
    this.processSpecifications();
  }

  private processSpecifications(): void {
    if (!this.comparison) return;

    const specifications: SpecificationDisplayDto[] = [];

    // Process differences
    this.comparison.differences?.forEach(diff => {
      if (diff.specificationKey && diff.displayName && diff.category) {
        specifications.push({
          key: diff.specificationKey,
          displayName: this.formatDisplayName(diff.displayName),
          category: diff.category,
          product1Value: this.formatSpecificationValue(diff.product1DisplayValue, diff.product1Value),
          product2Value: this.formatSpecificationValue(diff.product2DisplayValue, diff.product2Value),
          comparisonResult: diff.comparisonResult || ComparisonResultType._5, // Default to incomparable
          impactScore: diff.impactScore,
          analysisNote: diff.analysisNote,
          isMatch: false
        });
      }
    });

    // Process matches
    this.comparison.matches?.forEach(match => {
      if (match.specificationKey && match.displayName && match.category) {
        specifications.push({
          key: match.specificationKey,
          displayName: this.formatDisplayName(match.displayName),
          category: match.category,
          product1Value: this.formatSpecificationValue(match.displayValue, match.value),
          product2Value: this.formatSpecificationValue(match.displayValue, match.value),
          comparisonResult: ComparisonResultType._0, // Equivalent
          isMatch: true
        });
      }
    });

    this.dataSource.data = specifications;
    this.categoryScores = this.comparison.categoryScores || {};

    // Extract unique categories
    this.categories = Array.from(new Set(specifications.map(spec => spec.category))).sort();
  }

  private formatDisplayName(displayName: string): string {
    // If the display name is already in a good format, return it
    if (displayName && !displayName.includes('_')) {
      return displayName;
    }

    // Otherwise, format from snake_case to Title Case
    return displayName
      .split('_')
      .map(word => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
      .join(' ');
  }

  private formatSpecificationValue(displayValue: string | null | undefined, value: any): string {
    // Use display value if available
    if (displayValue) {
      return String(displayValue);
    }

    // Handle objects from normalized specifications
    if (value && typeof value === 'object' && value.value !== undefined) {
      return String(value.value);
    }

    // Fallback to the raw value
    return String(value || 'N/A');
  }

  getFilteredData(): SpecificationDisplayDto[] {
    if (this.selectedCategory === 'all') {
      return this.dataSource.data;
    }
    return this.dataSource.data.filter(spec => spec.category === this.selectedCategory);
  }

  getComparisonIcon(result: ComparisonResultType): string {
    switch (result) {
      case ComparisonResultType._1: // Product1Better
        return 'trending_up';
      case ComparisonResultType._2: // Product2Better
        return 'trending_down';
      case ComparisonResultType._0: // Equivalent
        return 'check_circle';
      case ComparisonResultType._3: // Product1Only
        return 'add_circle';
      case ComparisonResultType._4: // Product2Only
        return 'remove_circle';
      case ComparisonResultType._5: // Incomparable
        return 'help';
      default:
        return 'help';
    }
  }

  getComparisonClass(result: ComparisonResultType): string {
    switch (result) {
      case ComparisonResultType._1: // Product1Better
        return 'product1-better';
      case ComparisonResultType._2: // Product2Better
        return 'product2-better';
      case ComparisonResultType._0: // Equivalent
        return 'equivalent';
      case ComparisonResultType._3: // Product1Only
        return 'product1-only';
      case ComparisonResultType._4: // Product2Only
        return 'product2-only';
      case ComparisonResultType._5: // Incomparable
        return 'incomparable';
      default:
        return 'incomparable';
    }
  }

  getComparisonTooltip(result: ComparisonResultType): string {
    switch (result) {
      case ComparisonResultType._1: // Product1Better
        return `${this.getProductName(this.product1)} has better value`;
      case ComparisonResultType._2: // Product2Better
        return `${this.getProductName(this.product2)} has better value`;
      case ComparisonResultType._0: // Equivalent
        return 'Both products have equivalent values';
      case ComparisonResultType._3: // Product1Only
        return `Only ${this.getProductName(this.product1)} has this specification`;
      case ComparisonResultType._4: // Product2Only
        return `Only ${this.getProductName(this.product2)} has this specification`;
      case ComparisonResultType._5: // Incomparable
        return 'Values cannot be directly compared';
      default:
        return 'Unknown comparison result';
    }
  }

  getProductName(product?: ProductWithCurrentPricesDto): string {
    if (!product) return 'Unknown Product';
    return `${product.manufacturer || ''} ${product.name || ''}`.trim();
  }

  getImpactLevel(score?: number): string {
    if (!score) return 'low';
    if (score >= 0.7) return 'high';
    if (score >= 0.4) return 'medium';
    return 'low';
  }

  getCategoryScore(category: string): CategoryScoreDto | null {
    return this.categoryScores[category] || null;
  }

  getSpecificationsByCategory(): { [category: string]: SpecificationDisplayDto[] } {
    const result: { [category: string]: SpecificationDisplayDto[] } = {};

    this.dataSource.data.forEach(spec => {
      if (!result[spec.category]) {
        result[spec.category] = [];
      }
      result[spec.category].push(spec);
    });

    return result;
  }

  getDifferencesCount(): number {
    return this.dataSource.data.filter(spec => !spec.isMatch).length;
  }

  getMatchesCount(): number {
    return this.dataSource.data.filter(spec => spec.isMatch).length;
  }
}
