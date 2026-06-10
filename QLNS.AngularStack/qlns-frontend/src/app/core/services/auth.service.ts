import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class AuthService {
  role = signal<string | null>(null);
  username = signal<string | null>(null);
  fullName = signal<string | null>(null);
  private http = inject(HttpClient);

  constructor() {
    this.loadAuth();
  }

  loadAuth() {
    if (typeof localStorage !== 'undefined') {
      const savedRole = localStorage.getItem('role');
      const savedUsername = localStorage.getItem('username');
      const savedFullName = localStorage.getItem('fullName');
      if (savedRole) {
        this.role.set(savedRole);
      }
      if (savedUsername) {
        this.username.set(savedUsername);
      }
      if (savedFullName) {
        this.fullName.set(savedFullName);
      }
    }
  }

  login(username: string, password: string) {
    return this.http.post<{username: string, role: string, fullName: string}>('http://localhost:5294/api/Auth/login', {
      username: username,
      password: password
    }).pipe(
      tap(res => {
        localStorage.setItem('role', res.role);
        localStorage.setItem('username', res.username);
        localStorage.setItem('fullName', res.fullName);
        this.role.set(res.role);
        this.username.set(res.username);
        this.fullName.set(res.fullName);
      })
    );
  }

  register(user: any) {
    return this.http.post('http://localhost:5294/api/Auth/register', user);
  }

  logout() {
    localStorage.removeItem('role');
    localStorage.removeItem('username');
    localStorage.removeItem('fullName');
    this.role.set(null);
    this.username.set(null);
    this.fullName.set(null);
  }
}
