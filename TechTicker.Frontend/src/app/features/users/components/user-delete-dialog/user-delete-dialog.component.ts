import { Component, Inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { UserDto } from '../../../../shared/api/api-client';

@Component({
  selector: 'app-user-delete-dialog',
  templateUrl: './user-delete-dialog.component.html',
  styleUrls: ['./user-delete-dialog.component.scss'],
  standalone: false
})
export class UserDeleteDialogComponent {

  constructor(
    public dialogRef: MatDialogRef<UserDeleteDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: UserDto
  ) {}

  onCancel(): void {
    this.dialogRef.close(false);
  }

  onConfirm(): void {
    this.dialogRef.close(true);
  }

  getUserDisplayName(): string {
    if (this.data.fullName) {
      return this.data.fullName;
    }
    if (this.data.firstName || this.data.lastName) {
      return `${this.data.firstName || ''} ${this.data.lastName || ''}`.trim();
    }
    return this.data.email || 'Unknown User';
  }
}
