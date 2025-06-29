import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';

import { AppLayoutComponent } from './components/layout/app-layout.component';
import { ImageGalleryComponent } from './components/image-gallery/image-gallery.component';
import { RbacModule } from './modules/rbac.module';

@NgModule({
  declarations: [
    AppLayoutComponent,
    ImageGalleryComponent,
  ],
  imports: [
    CommonModule,
    RouterModule,
    MatToolbarModule,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    MatDividerModule,
    MatTooltipModule,
    RbacModule
  ],
  exports: [
    AppLayoutComponent,
    ImageGalleryComponent,
    RbacModule,
  ]
})
export class SharedModule { }
