import { Component, Inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { ProductDto } from '../../../../shared/api/api-client';

@Component({
  selector: 'app-product-delete-dialog',
  templateUrl: './product-delete-dialog.component.html',
  styleUrls: ['./product-delete-dialog.component.scss'],
  standalone: false
})
export class ProductDeleteDialogComponent {

  constructor(
    public dialogRef: MatDialogRef<ProductDeleteDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: ProductDto
  ) {}

  onCancel(): void {
    this.dialogRef.close(false);
  }

  onConfirm(): void {
    this.dialogRef.close(true);
  }
}
