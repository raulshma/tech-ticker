import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NzMessageService } from 'ng-zorro-antd/message';
import { finalize } from 'rxjs/operators';
import { UserManagementService, UserDetails, UpdateUserRequest } from '../../services/user-management.service';

@Component({
  selector: 'app-user-edit',
  templateUrl: './user-edit.component.html',
  styleUrls: ['./user-edit.component.css'],
  standalone: false
})
export class UserEditComponent implements OnInit {
  editForm!: FormGroup;
  user: UserDetails | null = null;
  loading = false;
  saving = false;
  userId!: string;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private userManagementService: UserManagementService,
    private message: NzMessageService
  ) {
    this.initializeForm();
  }

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.userId = params['id'];
      if (this.userId) {
        this.loadUserDetails();
      }
    });
  }

  private initializeForm(): void {
    this.editForm = this.fb.group({
      firstName: ['', [Validators.maxLength(100)]],
      lastName: ['', [Validators.maxLength(100)]],
      email: [{ value: '', disabled: true }] // Email is read-only for security
    });
  }

  loadUserDetails(): void {
    this.loading = true;

    this.userManagementService.getUserById(this.userId)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.user = response.data;
            this.populateForm();
          } else {
            this.message.error('Failed to load user details');
            this.goBack();
          }
        },
        error: (error) => {
          console.error('Error loading user details:', error);
          this.message.error('Error loading user details');
          this.goBack();
        }
      });
  }

  private populateForm(): void {
    if (this.user) {
      this.editForm.patchValue({
        firstName: this.user.firstName || '',
        lastName: this.user.lastName || '',
        email: this.user.email
      });
    }
  }

  onSubmit(): void {
    if (this.editForm.valid) {
      this.saving = true;

      const updateData: UpdateUserRequest = {
        firstName: this.editForm.value.firstName?.trim() || undefined,
        lastName: this.editForm.value.lastName?.trim() || undefined
      };

      // Remove undefined values
      Object.keys(updateData).forEach(key => {
        if (updateData[key as keyof UpdateUserRequest] === undefined) {
          delete updateData[key as keyof UpdateUserRequest];
        }
      });

      this.userManagementService.updateUser(this.userId, updateData)
        .pipe(finalize(() => this.saving = false))
        .subscribe({
          next: (response) => {
            if (response.success) {
              this.message.success('User updated successfully');
              this.router.navigate(['/users', this.userId]);
            } else {
              this.message.error('Failed to update user');
            }
          },
          error: (error) => {
            console.error('Error updating user:', error);
            this.message.error('Error updating user');
          }
        });
    } else {
      this.markFormGroupTouched();
    }
  }

  private markFormGroupTouched(): void {
    Object.keys(this.editForm.controls).forEach(key => {
      const control = this.editForm.get(key);
      control?.markAsTouched();
      control?.updateValueAndValidity();
    });
  }

  onCancel(): void {
    this.goBack();
  }

  goBack(): void {
    this.router.navigate(['/users', this.userId]);
  }

  goToUsersList(): void {
    this.router.navigate(['/users']);
  }

  // Form validation helpers
  isFieldInvalid(fieldName: string): boolean {
    const field = this.editForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.editForm.get(fieldName);
    if (field && field.errors) {
      if (field.errors['required']) {
        return `${this.getFieldLabel(fieldName)} is required`;
      }
      if (field.errors['maxlength']) {
        return `${this.getFieldLabel(fieldName)} cannot exceed ${field.errors['maxlength'].requiredLength} characters`;
      }
      if (field.errors['email']) {
        return 'Please enter a valid email address';
      }
    }
    return '';
  }

  private getFieldLabel(fieldName: string): string {
    const labels: { [key: string]: string } = {
      firstName: 'First Name',
      lastName: 'Last Name',
      email: 'Email Address'
    };
    return labels[fieldName] || fieldName;
  }

  getFullName(): string {
    if (!this.user) return '';

    const firstName = this.user.firstName || '';
    const lastName = this.user.lastName || '';

    if (firstName && lastName) {
      return `${firstName} ${lastName}`;
    }

    return firstName || lastName || this.user.email;
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleString();
  }
}
