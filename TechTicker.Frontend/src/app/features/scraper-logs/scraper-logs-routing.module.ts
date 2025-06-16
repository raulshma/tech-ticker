import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ScraperLogsListComponent } from './components/scraper-logs-list/scraper-logs-list.component';
import { ScraperLogDetailComponent } from './components/scraper-log-detail/scraper-log-detail.component';

const routes: Routes = [
  { path: '', component: ScraperLogsListComponent },
  { path: ':runId', component: ScraperLogDetailComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ScraperLogsRoutingModule { }
