import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Position, Employee } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class PositionService {
  private apiUrl = 'http://localhost:5294/api/Positions';

  constructor(private http: HttpClient) { }

  getPositions(): Observable<Position[]> {
    return this.http.get<Position[]>(this.apiUrl);
  }

  getPosition(id: number): Observable<{ position: Position, employees: Employee[] }> {
    return this.http.get<{ position: Position, employees: Employee[] }>(`${this.apiUrl}/${id}`);
  }

  createPosition(position: Position): Observable<Position> {
    return this.http.post<Position>(this.apiUrl, position);
  }

  updatePosition(id: number, position: Position): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, position);
  }

  deletePosition(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
