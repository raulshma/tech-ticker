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
import { MatCardModule } from '@angular/material/card';
import { MatDialogModule } from '@angular/material/dialog';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatChipsModule } from '@angular/material/chips';

import { SiteConfigsRoutingModule } from './site-configs-routing.module';
import { SiteConfigsListComponent } from './components/site-configs-list/site-configs-list.component';
import { SiteConfigFormComponent } from './components/site-config-form/site-config-form.component';
import { SiteConfigDeleteDialogComponent } from './components/site-config-delete-dialog/site-config-delete-dialog.component';

@NgModule({
  declarations: [
    SiteConfigsListComponent,
    SiteConfigFormComponent,
    SiteConfigDeleteDialogComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    SiteConfigsRoutingModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatCardModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatSlideToggleModule,
    MatExpansionModule,
    MatChipsModule
  ]
})
export class SiteConfigsModule { }
