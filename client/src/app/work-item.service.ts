import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
export type Status = 'Todo' | 'InProgress' | 'Done';
export interface WorkItem { id: number; title: string; description?: string | null; status: Status; createdAt: string; }
export interface Page<T> { items: T[]; page: number; pageSize: number; totalCount: number; }
@Injectable({ providedIn: 'root' }) export class WorkItemService {
  private http = inject(HttpClient);
  private api = '/api/work-items';
  list(title: string, status: string, page: number): Observable<Page<WorkItem>> {
    let params = new HttpParams().set('page', page).set('pageSize', 20);
    if (title.trim()) params = params.set('title', title.trim());
    if (status) params = params.set('status', status);
    return this.http.get<Page<WorkItem>>(this.api, { params });
  }
  create(title: string, description: string): Observable<WorkItem> { return this.http.post<WorkItem>(this.api, { title, description: description || null }); }
  advance(item: WorkItem): Observable<WorkItem> {
    const status: Status = item.status === 'Todo' ? 'InProgress' : 'Done';
    return this.http.patch<WorkItem>(`${this.api}/${item.id}/status`, { status });
  }
}
