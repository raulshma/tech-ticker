import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CategoryDto } from '../../../../shared/api/api-client';

@Component({
  selector: 'app-category-filter',
  templateUrl: './category-filter.component.html',
  styleUrls: ['./category-filter.component.scss'],
  standalone: false
})
export class CategoryFilterComponent {
  @Input() categories: CategoryDto[] = [];
  @Input() selectedCategoryId: string | null = null;
  @Output() categorySelected = new EventEmitter<string | null>();

  onCategoryClick(categoryId: string | null): void {
    this.categorySelected.emit(categoryId);
  }

  isSelected(categoryId: string): boolean {
    return this.selectedCategoryId === categoryId;
  }
}
