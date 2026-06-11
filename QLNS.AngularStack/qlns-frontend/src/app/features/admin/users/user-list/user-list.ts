import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserAccount, UserService } from '../user.service';

@Component({
  selector: 'app-user-list',
  imports: [CommonModule, FormsModule],
  templateUrl: './user-list.html',
  styleUrl: './user-list.css'
})
export class UserList implements OnInit {
  private userService = inject(UserService);

  users: UserAccount[] = [];
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.userService.getUsers().subscribe({
      next: (users: UserAccount[]) => {
        this.users = users;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Không thể tải danh sách tài khoản.';
        this.isLoading = false;
      }
    });
  }

  changeRole(user: UserAccount, newRole: string): void {
    if (user.role === newRole) {
      return;
    }

    const confirmed = confirm(`Bạn có chắc muốn đổi quyền của ${user.username} thành ${newRole}?`);

    if (!confirmed) {
      return;
    }

    this.errorMessage = '';
    this.successMessage = '';

    this.userService.updateRole(user.id, newRole).subscribe({
      next: (updatedUser: UserAccount) => {
        user.role = updatedUser.role;
        this.successMessage = 'Cập nhật quyền thành công.';
      },
      error: () => {
        this.errorMessage = 'Cập nhật quyền thất bại.';
      }
    });
  }

  resetPassword(user: UserAccount): void {
    const newPassword = prompt(`Nhập mật khẩu mới cho tài khoản ${user.username}:`);

    if (!newPassword) {
      return;
    }

    if (newPassword.length < 6) {
      this.errorMessage = 'Mật khẩu mới phải có ít nhất 6 ký tự.';
      return;
    }

    this.errorMessage = '';
    this.successMessage = '';

    this.userService.resetPassword(user.id, newPassword).subscribe({
      next: () => {
        this.successMessage = 'Reset mật khẩu thành công.';
      },
      error: () => {
        this.errorMessage = 'Reset mật khẩu thất bại.';
      }
    });
  }
}