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
import { MatTabsModule } from '@angular/material/tabs';
import { MatCardModule } from '@angular/material/card';
import { MatBadgeModule } from '@angular/material/badge';

import { AppLayoutComponent } from './components/layout/app-layout.component';
import { ImageGalleryComponent } from './components/image-gallery/image-gallery.component';
import { AiAssistantComponent } from './components/ai-assistant/ai-assistant.component';
import { ProductSpecificationsComponent } from './components/product-specifications/product-specifications.component';
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
    MatTabsModule,
    MatCardModule,
    MatBadgeModule,
    RbacModule,
    AiAssistantComponent,
    ProductSpecificationsComponent
  ],
  exports: [
    AppLayoutComponent,
    ImageGalleryComponent,
    ProductSpecificationsComponent,
    AiAssistantComponent,
    RbacModule,
  ]
})
export class SharedModule { }
