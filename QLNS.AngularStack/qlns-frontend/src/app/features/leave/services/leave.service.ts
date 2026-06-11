import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LeaveRequest, LeaveRequestCreateDto, ProcessRequestDto } from '../models/leave-request.model';
import { MyDaysOffResponse } from '../models/day-off-item.model';

@Injectable({
  providedIn: 'root'
})
export class LeaveService {
  private apiUrl = 'http://localhost:5294/api/LeaveRequests';

  constructor(private http: HttpClient) { }

  getMyLeaves(username: string): Observable<LeaveRequest[]> {
    return this.http.get<LeaveRequest[]>(`${this.apiUrl}/my-leaves/${username}`);
  }

  createLeaveRequest(username: string, model: LeaveRequestCreateDto): Observable<{message: string}> {
    return this.http.post<{message: string}>(`${this.apiUrl}/my-leaves/${username}`, model);
  }

  getMyDaysOff(username: string, year?: number): Observable<MyDaysOffResponse> {
    let url = `${this.apiUrl}/my-days-off/${username}`;
    if (year) {
      url += `?year=${year}`;
    }
    return this.http.get<MyDaysOffResponse>(url);
  }

  getApprovalList(): Observable<LeaveRequest[]> {
    return this.http.get<LeaveRequest[]>(`${this.apiUrl}/approval`);
  }

  processRequest(id: number, data: ProcessRequestDto): Observable<any> {
    return this.http.put(`${this.apiUrl}/approval/${id}`, data);
  }
}
