import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/api-config';
import { InquiryResponse, InquirySummary, PagedResult, PriorityResponse, StatusResponse } from '../models/inquiry.model';
import { InquiryFilterRequest } from '../models/query-params.model';

// HTTP calls only — returns Observables, no UI state, no subscribe() inside it.
// One method per Inquires.Api endpoint; keep this file a 1:1 mirror of the controller.
@Injectable({ providedIn: 'root' })
export class InquiryService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/inquiries`;

  getInquiries(filter: InquiryFilterRequest): Observable<PagedResult<InquiryResponse>> {
    return this.http.get<PagedResult<InquiryResponse>>(this.baseUrl, { params: toHttpParams(filter) });
  }

  getInquiryById(id: number): Observable<InquiryResponse> {
    return this.http.get<InquiryResponse>(`${this.baseUrl}/${id}`);
  }

  updateStatus(id: number, statusId: number): Observable<InquiryResponse> {
    return this.http.patch<InquiryResponse>(`${this.baseUrl}/${id}/status`, { statusId });
  }

  getSummary(): Observable<InquirySummary[]> {
    return this.http.get<InquirySummary[]>(`${this.baseUrl}/summary`);
  }

  getStatuses(): Observable<StatusResponse[]> {
    return this.http.get<StatusResponse[]>(`${this.baseUrl}/statuses`);
  }

  getPriorities(): Observable<PriorityResponse[]> {
    return this.http.get<PriorityResponse[]>(`${this.baseUrl}/priorities`);
  }
}

function toHttpParams(filter: InquiryFilterRequest): HttpParams {
  let params = new HttpParams()
    .set('sortBy', filter.sortBy)
    .set('sortDescending', String(filter.sortDescending))
    .set('pageNumber', String(filter.pageNumber))
    .set('pageSize', String(filter.pageSize));

  if (filter.searchTerm) params = params.set('searchTerm', filter.searchTerm);
  if (filter.statusId != null) params = params.set('statusId', String(filter.statusId));
  if (filter.priorityId != null) params = params.set('priorityId', String(filter.priorityId));
  if (filter.organizationName) params = params.set('organizationName', filter.organizationName);

  return params;
}
