import { Component, Inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { ScraperSiteConfigurationDto } from '../../../../shared/api/api-client';

@Component({
  selector: 'app-site-config-delete-dialog',
  templateUrl: './site-config-delete-dialog.component.html',
  styleUrls: ['./site-config-delete-dialog.component.scss'],
  standalone: false
})
export class SiteConfigDeleteDialogComponent {

  constructor(
    public dialogRef: MatDialogRef<SiteConfigDeleteDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: ScraperSiteConfigurationDto
  ) {}

  onCancel(): void {
    this.dialogRef.close(false);
  }

  onConfirm(): void {
    this.dialogRef.close(true);
  }
}
