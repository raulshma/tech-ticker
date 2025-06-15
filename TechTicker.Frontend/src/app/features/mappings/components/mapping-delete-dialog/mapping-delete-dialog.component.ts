import { Component, Inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { ProductSellerMappingDto } from '../../../../shared/api/api-client';

@Component({
  selector: 'app-mapping-delete-dialog',
  templateUrl: './mapping-delete-dialog.component.html',
  styleUrls: ['./mapping-delete-dialog.component.scss'],
  standalone: false
})
export class MappingDeleteDialogComponent {

  constructor(
    public dialogRef: MatDialogRef<MappingDeleteDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: ProductSellerMappingDto
  ) {}

  onCancel(): void {
    this.dialogRef.close(false);
  }

  onConfirm(): void {
    this.dialogRef.close(true);
  }
}
