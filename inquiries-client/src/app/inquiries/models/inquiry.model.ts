export interface StatusResponse {
  statusId: number;
  name: string;
}

export interface PriorityResponse {
  priorityId: number;
  name: string;
}

export interface InquiryResponse {
  inquiryId: number;
  title: string;
  organizationName: string;
  statusId: number;
  statusName: string;
  priorityId: number;
  priorityName: string;
  createdAt: string;
  updatedAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface InquirySummary {
  statusName: string;
  count: number;
}
