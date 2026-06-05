import { Component, inject } from '@angular/core';
import { RouterLink, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-login',
  imports: [RouterLink, FormsModule, CommonModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  username = '';
  password = '';
  auth = inject(AuthService);
  router = inject(Router);

  onLogin() {
    if (this.username) {
        this.auth.login(this.username, this.password).subscribe({
            next: (res) => {
                if (res.role === 'Admin') {
                    this.router.navigate(['/admin-dashboard']);
                } else {
                    this.router.navigate(['/employee-dashboard']);
                }
            },
            error: (err) => {
                alert('Sai tài khoản hoặc mật khẩu.');
            }
        });
    }
  }
}
