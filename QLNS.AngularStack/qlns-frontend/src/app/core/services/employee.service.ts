import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Employee, ProfileUpdateRequest, Department } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class EmployeeService {
  private apiUrl = 'http://localhost:5294/api/Employees';

  constructor(private http: HttpClient) { }

  getEmployees(): Observable<Employee[]> {
    return this.http.get<Employee[]>(this.apiUrl);
  }

  getEmployee(id: number): Observable<Employee> {
    return this.http.get<Employee>(`${this.apiUrl}/${id}`);
  }

  createEmployee(employee: Employee): Observable<Employee> {
    return this.http.post<Employee>(this.apiUrl, employee);
  }

  updateEmployee(id: number, employee: Employee): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, employee);
  }

  deleteEmployee(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  getMyProfile(username: string): Observable<Employee> {
    return this.http.get<Employee>(`${this.apiUrl}/my-profile?username=${username}`);
  }

  getMyDepartment(username: string): Observable<{ department: Department, members: Employee[] }> {
    return this.http.get<{ department: Department, members: Employee[] }>(`${this.apiUrl}/my-department?username=${username}`);
  }

  requestProfileUpdate(request: Partial<ProfileUpdateRequest>): Observable<any> {
    return this.http.post(`${this.apiUrl}/request-update`, request);
  }

  getPendingUpdates(): Observable<ProfileUpdateRequest[]> {
    return this.http.get<ProfileUpdateRequest[]>(`${this.apiUrl}/pending-updates`);
  }

  approveUpdate(id: number, isApproved: boolean): Observable<any> {
    return this.http.post(`${this.apiUrl}/approve-update/${id}?isApproved=${isApproved}`, {});
  }
}
