import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminRoutingModule } from './admin-routing.module';
import { ProxyListComponent } from './proxy-management/proxy-list/proxy-list.component';
import { ProxyFormComponent } from './proxy-management/proxy-form/proxy-form.component';
import { BulkImportComponent } from './proxy-management/bulk-import/bulk-import.component';
import { AiSettingsComponent } from './ai-settings/ai-settings.component';
import { BrowserAutomationTesterComponent } from './browser-automation-tester/browser-automation-tester.component';

@NgModule({
  declarations: [],
  imports: [
    CommonModule,
    AdminRoutingModule,
    ProxyListComponent, // Import as standalone component
    ProxyFormComponent, // Import as standalone component
    BulkImportComponent, // Import as standalone component
    AiSettingsComponent, // Import as standalone component
    BrowserAutomationTesterComponent // Import as standalone component
  ]
})
export class AdminModule { }
