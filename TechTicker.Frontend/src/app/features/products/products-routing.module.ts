import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ProductsListComponent } from './components/products-list/products-list.component';
import { ProductFormComponent } from './components/product-form/product-form.component';
import { PriceHistoryComponent } from './components/price-history/price-history.component';

const routes: Routes = [
  { path: '', component: ProductsListComponent },
  { path: 'new', component: ProductFormComponent },
  { path: 'edit/:id', component: ProductFormComponent },
  { path: ':id/price-history', component: PriceHistoryComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ProductsRoutingModule { }
