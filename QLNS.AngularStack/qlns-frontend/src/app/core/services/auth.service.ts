import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class AuthService {
  role = signal<string | null>(null);
  private http = inject(HttpClient);

  constructor() {
    const savedRole = localStorage.getItem('role');
    if (savedRole) {
      this.role.set(savedRole);
    }
  }

  login(username: string, password: string) {
    return this.http.post<{username: string, role: string, fullName: string}>('http://localhost:5294/api/Auth/login', {
      username: username,
      password: password
    }).pipe(
      tap(res => {
        localStorage.setItem('role', res.role);
        this.role.set(res.role);
      })
    );
  }

  register(user: any) {
    return this.http.post('http://localhost:5294/api/Auth/register', user);
  }

  logout() {
    localStorage.removeItem('role');
    this.role.set(null);
  }
}
