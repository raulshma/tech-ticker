import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MappingsListComponent } from './components/mappings-list/mappings-list.component';
import { MappingFormComponent } from './components/mapping-form/mapping-form.component';

const routes: Routes = [
  { path: '', component: MappingsListComponent },
  { path: 'new', component: MappingFormComponent },
  { path: 'edit/:id', component: MappingFormComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class MappingsRoutingModule { }
