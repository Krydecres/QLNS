import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LeaveRequest, LeaveRequestCreateDto, ProcessRequestDto } from '../models/leave-request.model';

@Injectable({
  providedIn: 'root'
})
export class LeaveService {
  private apiUrl = 'http://localhost:5294/api/LeaveRequests';

  constructor(private http: HttpClient) { }

  getMyLeaves(username: string): Observable<LeaveRequest[]> {
    return this.http.get<LeaveRequest[]>(`${this.apiUrl}/my-leaves/${username}`);
  }

  createLeaveRequest(username: string, data: LeaveRequestCreateDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/my-leaves/${username}`, data);
  }

  getApprovalList(): Observable<LeaveRequest[]> {
    return this.http.get<LeaveRequest[]>(`${this.apiUrl}/approval`);
  }

  processRequest(id: number, data: ProcessRequestDto): Observable<any> {
    return this.http.put(`${this.apiUrl}/approval/${id}`, data);
  }
}
