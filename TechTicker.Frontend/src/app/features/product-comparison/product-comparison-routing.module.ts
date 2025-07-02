import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ProductComparisonViewComponent } from './components/product-comparison-view/product-comparison-view.component';

const routes: Routes = [
  { path: '', component: ProductComparisonViewComponent },
  { path: ':productId1/:productId2', component: ProductComparisonViewComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ProductComparisonRoutingModule { }