import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { 
  UserDto, 
  CreateUserDto, 
  UpdateUserDto 
} from '../../../../shared/api/api-client';
import { UsersService } from '../../services/users.service';

@Component({
  selector: 'app-user-form',
  templateUrl: './user-form.component.html',
  styleUrls: ['./user-form.component.scss'],
  standalone: false
})
export class UserFormComponent implements OnInit {
  userForm: FormGroup;
  isLoading = false;
  isEditMode = false;
  userId: string | null = null;
  availableRoles: string[] = [];
  hidePassword = true;

  constructor(
    private formBuilder: FormBuilder,
    private usersService: UsersService,
    private router: Router,
    private route: ActivatedRoute,
    private snackBar: MatSnackBar
  ) {
    this.userForm = this.formBuilder.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      firstName: ['', [Validators.maxLength(100)]],
      lastName: ['', [Validators.maxLength(100)]],
      roles: [[]],
      isActive: [true]
    });
  }

  ngOnInit(): void {
    this.userId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.userId;
    this.availableRoles = this.usersService.getAvailableRoles();

    if (this.isEditMode) {
      // Remove password requirement for edit mode
      this.userForm.get('password')?.clearValidators();
      this.userForm.get('password')?.updateValueAndValidity();
      
      if (this.userId) {
        this.loadUser(this.userId);
      }
    }
  }

  loadUser(id: string): void {
    this.isLoading = true;
    this.usersService.getUser(id).subscribe({
      next: (user) => {
        this.userForm.patchValue({
          email: user.email,
          firstName: user.firstName,
          lastName: user.lastName,
          roles: user.roles || [],
          isActive: user.isActive
        });
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading user:', error);
        this.snackBar.open('Failed to load user', 'Close', { duration: 5000 });
        this.router.navigate(['/users']);
      }
    });
  }

  onSubmit(): void {
    if (this.userForm.valid && !this.isLoading) {
      this.isLoading = true;

      const formValue = this.userForm.value;
      
      if (this.isEditMode && this.userId) {
        const updateDto = new UpdateUserDto({
          email: formValue.email,
          firstName: formValue.firstName || undefined,
          lastName: formValue.lastName || undefined,
          roles: formValue.roles && formValue.roles.length > 0 ? formValue.roles : undefined,
          isActive: formValue.isActive
        });

        this.usersService.updateUser(this.userId, updateDto).subscribe({
          next: () => {
            this.snackBar.open('User updated successfully', 'Close', { duration: 3000 });
            this.router.navigate(['/users']);
          },
          error: (error) => {
            console.error('Error updating user:', error);
            this.snackBar.open('Failed to update user', 'Close', { duration: 5000 });
            this.isLoading = false;
          }
        });
      } else {
        const createDto = new CreateUserDto({
          email: formValue.email,
          password: formValue.password,
          firstName: formValue.firstName || undefined,
          lastName: formValue.lastName || undefined,
          roles: formValue.roles && formValue.roles.length > 0 ? formValue.roles : undefined
        });

        this.usersService.createUser(createDto).subscribe({
          next: () => {
            this.snackBar.open('User created successfully', 'Close', { duration: 3000 });
            this.router.navigate(['/users']);
          },
          error: (error) => {
            console.error('Error creating user:', error);
            this.snackBar.open('Failed to create user', 'Close', { duration: 5000 });
            this.isLoading = false;
          }
        });
      }
    } else {
      this.markFormGroupTouched();
    }
  }

  onCancel(): void {
    this.router.navigate(['/users']);
  }

  togglePasswordVisibility(): void {
    this.hidePassword = !this.hidePassword;
  }

  private markFormGroupTouched(): void {
    Object.keys(this.userForm.controls).forEach(key => {
      const control = this.userForm.get(key);
      if (control) {
        control.markAsTouched();
      }
    });
  }

  getFieldErrorMessage(fieldName: string): string {
    const control = this.userForm.get(fieldName);
    if (control?.hasError('required')) {
      return `${fieldName.charAt(0).toUpperCase() + fieldName.slice(1)} is required`;
    }
    if (control?.hasError('email')) {
      return 'Please enter a valid email address';
    }
    if (control?.hasError('minlength')) {
      const minLength = control.errors?.['minlength']?.requiredLength;
      return `${fieldName.charAt(0).toUpperCase() + fieldName.slice(1)} must be at least ${minLength} characters`;
    }
    if (control?.hasError('maxlength')) {
      const maxLength = control.errors?.['maxlength']?.requiredLength;
      return `${fieldName.charAt(0).toUpperCase() + fieldName.slice(1)} must be less than ${maxLength} characters`;
    }
    return '';
  }
}
