import { Component, inject } from '@angular/core';
import { RouterLink, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-register',
  imports: [RouterLink, FormsModule, CommonModule],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  user = {
    username: '',
    fullName: '',
    email: '',
    password: '',
    confirmPassword: '',
    role: 'Employee'
  };

  auth = inject(AuthService);
  router = inject(Router);

  onRegister() {
    if (this.user.password !== this.user.confirmPassword) {
      alert('Mật khẩu xác nhận không khớp!');
      return;
    }

    if (this.user.username && this.user.password && this.user.fullName) {
      this.auth.register(this.user).subscribe({
        next: () => {
          alert('Đăng ký thành công!');
          this.router.navigate(['/login']);
        },
        error: (err) => {
          alert(err.error?.message || 'Đăng ký thất bại!');
        }
      });
    }
  }
}
