import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface SalaryItem {
  id: number;
  employeeId: number;
  employeeName: string;
  positionName: string;
  month: number;
  year: number;
  baseSalary: number;
  allowance: number;
  deduction: number;
  totalSalary: number;
}

@Injectable({
  providedIn: 'root'
})
export class SalaryService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5294/api/Salaries';

  getSalaries(month: number, year: number): Observable<SalaryItem[]> {
    const params = new HttpParams()
      .set('month', month)
      .set('year', year);

    return this.http.get<SalaryItem[]>(this.apiUrl, { params });
  }

  calculateSalaries(month: number, year: number): Observable<{ message: string }> {
    const params = new HttpParams()
      .set('month', month)
      .set('year', year);

    return this.http.post<{ message: string }>(
      `${this.apiUrl}/calculate`,
      {},
      { params }
    );
  }

  exportExcel(month: number, year: number): Observable<Blob> {
    const params = new HttpParams()
      .set('month', month)
      .set('year', year);

    return this.http.get(`${this.apiUrl}/export`, {
      params,
      responseType: 'blob'
    });
  }

  bulkSendEmail(month: number, year: number): Observable<{ message: string }> {
    const params = new HttpParams()
      .set('month', month)
      .set('year', year);

    return this.http.post<{ message: string }>(
      `${this.apiUrl}/bulk-send-email`,
      {},
      { params }
    );
  }

  clearSalaries(month: number, year: number): Observable<{ message: string }> {
    const params = new HttpParams()
      .set('month', month)
      .set('year', year);

    return this.http.delete<{ message: string }>(
      `${this.apiUrl}/clear`,
      { params }
    );
  }
}