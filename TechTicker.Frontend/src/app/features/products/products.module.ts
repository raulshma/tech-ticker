import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatSortModule } from '@angular/material/sort';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
import { MatDialogModule } from '@angular/material/dialog';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { BaseChartDirective } from 'ng2-charts';

import { ProductsRoutingModule } from './products-routing.module';
import { ProductsListComponent } from './components/products-list/products-list.component';
import { ProductFormComponent } from './components/product-form/product-form.component';
import { ProductDeleteDialogComponent } from './components/product-delete-dialog/product-delete-dialog.component';
import { PriceHistoryComponent } from './components/price-history/price-history.component';

@NgModule({
  declarations: [
    ProductsListComponent,
    ProductFormComponent,
    ProductDeleteDialogComponent,
    PriceHistoryComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ProductsRoutingModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCardModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatChipsModule,
    MatSlideToggleModule,
    MatButtonToggleModule,
    BaseChartDirective
  ]
})
export class ProductsModule { }
