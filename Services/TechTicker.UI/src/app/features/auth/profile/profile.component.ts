import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzModalService } from 'ng-zorro-antd/modal';
import { AuthService, User, UpdateProfileRequest, ChangePasswordRequest } from '../../../core/services/auth.service';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css'],
  standalone: false
})
export class ProfileComponent implements OnInit {
  profileForm!: FormGroup;
  passwordForm!: FormGroup;
  currentUser: User | null = null;
  isProfileLoading = false;
  isPasswordLoading = false;
  passwordVisible = false;
  newPasswordVisible = false;
  confirmPasswordVisible = false;
  activeTab = 0;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private message: NzMessageService,
    private modal: NzModalService
  ) {}

  ngOnInit(): void {
    this.initForms();
    this.loadUserProfile();
  }

  private initForms(): void {
    this.profileForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]]
    });

    this.passwordForm = this.fb.group({
      currentPassword: ['', [Validators.required]],
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]]
    }, {
      validators: this.passwordMatchValidator
    });
  }

  private passwordMatchValidator(form: FormGroup) {
    const newPassword = form.get('newPassword');
    const confirmPassword = form.get('confirmPassword');

    if (newPassword && confirmPassword && newPassword.value !== confirmPassword.value) {
      return { passwordMismatch: true };
    }
    return null;
  }

  private loadUserProfile(): void {
    this.authService.getCurrentUser().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.currentUser = response.data;
          this.profileForm.patchValue({
            firstName: this.currentUser.firstName,
            lastName: this.currentUser.lastName,
            email: this.currentUser.email
          });
        }
      },
      error: (error) => {
        this.message.error('Failed to load profile information');
      }
    });
  }

  onUpdateProfile(): void {
    if (this.profileForm.valid) {
      this.isProfileLoading = true;
      const profileData: UpdateProfileRequest = this.profileForm.value;

      this.authService.updateProfile(profileData).subscribe({
        next: (response) => {
          this.isProfileLoading = false;
          if (response.success) {
            this.message.success('Profile updated successfully!');
            this.currentUser = response.data;
          } else {
            this.message.error(response.message || 'Failed to update profile');
          }
        },
        error: (error) => {
          this.isProfileLoading = false;
          const errorMessage = error.error?.message || 'Failed to update profile';
          this.message.error(errorMessage);
        }
      });
    } else {
      this.markFormGroupTouched(this.profileForm);
    }
  }

  onChangePassword(): void {
    if (this.passwordForm.valid) {
      this.isPasswordLoading = true;
      const passwordData: ChangePasswordRequest = {
        currentPassword: this.passwordForm.value.currentPassword,
        newPassword: this.passwordForm.value.newPassword
      };

      this.authService.changePassword(passwordData).subscribe({
        next: (response) => {
          this.isPasswordLoading = false;
          if (response.success) {
            this.message.success('Password changed successfully!');
            this.passwordForm.reset();
          } else {
            this.message.error(response.message || 'Failed to change password');
          }
        },
        error: (error) => {
          this.isPasswordLoading = false;
          const errorMessage = error.error?.message || 'Failed to change password';
          this.message.error(errorMessage);
        }
      });
    } else {
      this.markFormGroupTouched(this.passwordForm);
    }
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      if (control) {
        control.markAsTouched();
        control.updateValueAndValidity();
      }
    });
  }

  isFieldInvalid(formGroup: FormGroup, fieldName: string): boolean {
    const field = formGroup.get(fieldName);
    return !!(field && field.invalid && field.touched);
  }

  getFieldError(formGroup: FormGroup, fieldName: string): string {
    const field = formGroup.get(fieldName);
    if (field && field.errors && field.touched) {
      if (field.errors['required']) {
        return `${this.getFieldDisplayName(fieldName)} is required`;
      }
      if (field.errors['email']) {
        return 'Please enter a valid email address';
      }
      if (field.errors['minlength']) {
        return `${this.getFieldDisplayName(fieldName)} must be at least ${field.errors['minlength'].requiredLength} characters`;
      }
    }

    // Check for password mismatch error
    if (fieldName === 'confirmPassword' && formGroup.errors?.['passwordMismatch']) {
      return 'Passwords do not match';
    }

    return '';
  }

  private getFieldDisplayName(fieldName: string): string {
    const displayNames: { [key: string]: string } = {
      firstName: 'First name',
      lastName: 'Last name',
      email: 'Email',
      currentPassword: 'Current password',
      newPassword: 'New password',
      confirmPassword: 'Confirm password'
    };
    return displayNames[fieldName] || fieldName;
  }

  resetProfileForm(): void {
    if (this.currentUser) {
      this.profileForm.patchValue({
        firstName: this.currentUser.firstName,
        lastName: this.currentUser.lastName,
        email: this.currentUser.email
      });
      this.profileForm.markAsUntouched();
    }
  }

  resetPasswordForm(): void {
    this.passwordForm.reset();
    this.passwordForm.markAsUntouched();
  }

  getUserInitials(): string {
    if (this.currentUser) {
      return `${this.currentUser.firstName.charAt(0)}${this.currentUser.lastName.charAt(0)}`.toUpperCase();
    }
    return 'U';
  }

  getRoleDisplayName(role: string): string {
    const roleNames: { [key: string]: string } = {
      'admin': 'Administrator',
      'user': 'User',
      'moderator': 'Moderator'
    };
    return roleNames[role.toLowerCase()] || role;
  }
}
