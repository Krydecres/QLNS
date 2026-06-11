import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface UserAccount {
  id: number;
  username: string;
  fullName: string;
  email: string;
  role: string;
}

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5294/api/Users';

  getUsers(): Observable<UserAccount[]> {
    return this.http.get<UserAccount[]>(this.apiUrl);
  }

  updateRole(id: number, role: string): Observable<UserAccount> {
    return this.http.put<UserAccount>(`${this.apiUrl}/${id}/role`, { role });
  }

  resetPassword(id: number, newPassword: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/${id}/reset-password`,
      { newPassword }
    );
  }
}