import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { NzMessageService } from 'ng-zorro-antd/message';
import { finalize } from 'rxjs/operators';
import { UserManagementService, CreateUserRequest } from '../../services/user-management.service';

@Component({
  selector: 'app-user-create',
  templateUrl: './user-create.component.html',
  styleUrls: ['./user-create.component.css'],
  standalone: false
})
export class UserCreateComponent implements OnInit {
  createForm!: FormGroup;
  creating = false;
  passwordVisible = false;

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private userManagementService: UserManagementService,
    private message: NzMessageService
  ) {
    this.initializeForm();
  }

  ngOnInit(): void {}

  private initializeForm(): void {
    this.createForm = this.fb.group({
      email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
      password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
      firstName: ['', [Validators.maxLength(100)]],
      lastName: ['', [Validators.maxLength(100)]]
    });
  }

  onSubmit(): void {
    if (this.createForm.valid) {
      this.creating = true;

      const createData: CreateUserRequest = {
        email: this.createForm.value.email.trim(),
        password: this.createForm.value.password,
        firstName: this.createForm.value.firstName?.trim() || undefined,
        lastName: this.createForm.value.lastName?.trim() || undefined
      };

      // Remove undefined values
      Object.keys(createData).forEach(key => {
        if (createData[key as keyof CreateUserRequest] === undefined) {
          delete createData[key as keyof CreateUserRequest];
        }
      });

      this.userManagementService.createUser(createData)
        .pipe(finalize(() => this.creating = false))
        .subscribe({
          next: (response) => {
            if (response.success && response.data) {
              this.message.success('User created successfully');
              this.router.navigate(['/users', response.data.userId]);
            } else {
              this.message.error('Failed to create user');
            }
          },
          error: (error) => {
            console.error('Error creating user:', error);
            if (error.error?.message) {
              this.message.error(error.error.message);
            } else {
              this.message.error('Error creating user');
            }
          }
        });
    } else {
      this.markFormGroupTouched();
    }
  }

  private markFormGroupTouched(): void {
    Object.keys(this.createForm.controls).forEach(key => {
      const control = this.createForm.get(key);
      control?.markAsTouched();
      control?.updateValueAndValidity();
    });
  }

  onCancel(): void {
    this.router.navigate(['/users']);
  }

  togglePasswordVisibility(): void {
    this.passwordVisible = !this.passwordVisible;
  }

  generatePassword(): void {
    const password = this.generateRandomPassword();
    this.createForm.patchValue({ password });
    this.message.success('Random password generated');
  }

  private generateRandomPassword(): string {
    const length = 12;
    const charset = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*';
    let password = '';

    // Ensure at least one of each type
    password += 'abcdefghijklmnopqrstuvwxyz'[Math.floor(Math.random() * 26)]; // lowercase
    password += 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'[Math.floor(Math.random() * 26)]; // uppercase
    password += '0123456789'[Math.floor(Math.random() * 10)]; // number
    password += '!@#$%^&*'[Math.floor(Math.random() * 8)]; // special

    // Fill the rest
    for (let i = password.length; i < length; i++) {
      password += charset[Math.floor(Math.random() * charset.length)];
    }

    // Shuffle the password
    return password.split('').sort(() => Math.random() - 0.5).join('');
  }

  // Form validation helpers
  isFieldInvalid(fieldName: string): boolean {
    const field = this.createForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.createForm.get(fieldName);
    if (field && field.errors) {
      if (field.errors['required']) {
        return `${this.getFieldLabel(fieldName)} is required`;
      }
      if (field.errors['email']) {
        return 'Please enter a valid email address';
      }
      if (field.errors['minlength']) {
        return `${this.getFieldLabel(fieldName)} must be at least ${field.errors['minlength'].requiredLength} characters`;
      }
      if (field.errors['maxlength']) {
        return `${this.getFieldLabel(fieldName)} cannot exceed ${field.errors['maxlength'].requiredLength} characters`;
      }
    }
    return '';
  }

  private getFieldLabel(fieldName: string): string {
    const labels: { [key: string]: string } = {
      email: 'Email Address',
      password: 'Password',
      firstName: 'First Name',
      lastName: 'Last Name'
    };
    return labels[fieldName] || fieldName;
  }

  getPasswordStrength(): { strength: string; color: string; percentage: number } {
    const password = this.createForm.get('password')?.value || '';
    let score = 0;

    if (password.length >= 8) score++;
    if (/[a-z]/.test(password)) score++;
    if (/[A-Z]/.test(password)) score++;
    if (/[0-9]/.test(password)) score++;
    if (/[^A-Za-z0-9]/.test(password)) score++;

    const strengthMap = [
      { strength: 'Very Weak', color: '#ff4d4f', percentage: 20 },
      { strength: 'Weak', color: '#ff7a45', percentage: 40 },
      { strength: 'Fair', color: '#ffa940', percentage: 60 },
      { strength: 'Good', color: '#52c41a', percentage: 80 },
      { strength: 'Strong', color: '#389e0d', percentage: 100 }
    ];

    return strengthMap[score] || strengthMap[0];
  }
}
