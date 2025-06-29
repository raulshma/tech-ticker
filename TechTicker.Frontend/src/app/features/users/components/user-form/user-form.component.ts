import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { Subject, takeUntil } from 'rxjs';
import { UserDto, CreateUserDto, UpdateUserDto } from '../../../../shared/api/api-client';
import { UsersService } from '../../services/users.service';
import { UserPermissionsComponent } from '../user-permissions/user-permissions.component';
import { RbacModule } from '../../../../shared/modules/rbac.module';

@Component({
  selector: 'app-user-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatChipsModule,
    MatSlideToggleModule,
    MatMenuModule,
    MatDividerModule,
    UserPermissionsComponent,
    RbacModule
  ],
  templateUrl: './user-form.component.html',
  styleUrls: ['./user-form.component.scss']
})
export class UserFormComponent implements OnInit {
  userForm: FormGroup;
  isEditMode = false;
  isLoading = false;
  currentUser: UserDto | null = null;
  hidePassword = true;
  availableRoles: string[] = [];

  private destroy$ = new Subject<void>();
  private userId: string | null = null;

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private route: ActivatedRoute,
    private usersService: UsersService,
    private snackBar: MatSnackBar
  ) {
    this.userForm = this.createForm();
  }

  ngOnInit(): void {
    this.loadAvailableRoles();
    this.checkRouteParams();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private createForm(): FormGroup {
    return this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      firstName: [''],
      lastName: [''],
      roles: [[]],
      isActive: [true]
    });
  }

  private checkRouteParams(): void {
    this.userId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.userId;

    if (this.isEditMode && this.userId) {
      this.loadUser(this.userId);
      // Remove password validation for edit mode
      this.userForm.get('password')?.clearValidators();
      this.userForm.get('password')?.updateValueAndValidity();
    }
  }

  private loadAvailableRoles(): void {
    // Get available roles from the users service
    const roleDisplayNames = this.usersService.getRoleDisplayNames();
    this.availableRoles = Object.keys(roleDisplayNames);
  }

  loadUser(id: string): void {
    this.isLoading = true;
    this.usersService.getUser(id).subscribe({
      next: (user) => {
        this.currentUser = user;
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
    if (this.userForm.valid) {
      this.isLoading = true;

      if (this.isEditMode) {
        this.updateUser();
      } else {
        this.createUser();
      }
    }
  }

  private createUser(): void {
    const formValue = this.userForm.value;
    const createUserDto = new CreateUserDto({
      email: formValue.email,
      password: formValue.password,
      firstName: formValue.firstName,
      lastName: formValue.lastName,
      roles: formValue.roles
    });

    this.usersService.createUser(createUserDto)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
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

  private updateUser(): void {
    if (!this.userId) return;

    const formValue = this.userForm.value;
    const updateUserDto = new UpdateUserDto({
      email: formValue.email,
      firstName: formValue.firstName,
      lastName: formValue.lastName,
      roles: formValue.roles,
      isActive: formValue.isActive
    });

    this.usersService.updateUser(this.userId, updateUserDto)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
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
  }

  togglePasswordVisibility(): void {
    this.hidePassword = !this.hidePassword;
  }

  onCancel(): void {
    this.router.navigate(['/users']);
  }

  getFieldErrorMessage(fieldName: string): string {
    const field = this.userForm.get(fieldName);
    if (field?.hasError('required')) {
      return `${this.getFieldLabel(fieldName)} is required`;
    }
    if (field?.hasError('email')) {
      return 'Please enter a valid email address';
    }
    if (field?.hasError('minlength')) {
      const minLength = field.errors?.['minlength']?.requiredLength;
      return `${this.getFieldLabel(fieldName)} must be at least ${minLength} characters long`;
    }
    return '';
  }

  private getFieldLabel(fieldName: string): string {
    const labels: { [key: string]: string } = {
      email: 'Email',
      password: 'Password',
      firstName: 'First Name',
      lastName: 'Last Name'
    };
    return labels[fieldName] || fieldName;
  }

  getRoleIcon(role: string): string {
    const iconMap: { [key: string]: string } = {
      'Admin': 'admin_panel_settings',
      'Manager': 'supervisor_account',
      'User': 'person',
      'Moderator': 'shield'
    };
    return iconMap[role] || 'person';
  }

  hasPermission(permission: string): boolean {
    // This would typically check the current user's permissions
    // For now, return true as a placeholder
    return true;
  }
}
