import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DocumentDto } from '../models/document.model';

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/documents`;

  list(): Observable<DocumentDto[]> {
    return this.http.get<DocumentDto[]>(this.baseUrl);
  }

  get(id: string): Observable<DocumentDto> {
    return this.http.get<DocumentDto>(`${this.baseUrl}/${id}`);
  }

  upload(file: File): Observable<DocumentDto> {
    const form = new FormData();
    // Field name must match the IFormFile parameter name on the controller.
    form.append('file', file, file.name);
    return this.http.post<DocumentDto>(this.baseUrl, form);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
