import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminRoutingModule } from './admin-routing.module';
import { ProxyListComponent } from './proxy-management/proxy-list/proxy-list.component';

@NgModule({
  declarations: [],
  imports: [
    CommonModule,
    AdminRoutingModule,
    ProxyListComponent // Import as standalone component
  ]
})
export class AdminModule { }
