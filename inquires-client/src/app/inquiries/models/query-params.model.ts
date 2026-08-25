// Mirrors the exact whitelist in InquiryRepository.ApplySort — keep in sync with the server switch.
export const SORTABLE_FIELDS = ['Title', 'OrganizationName', 'Status', 'Priority', 'CreatedAt', 'UpdatedAt'] as const;
export type SortableField = (typeof SORTABLE_FIELDS)[number];

export function isSortableField(value: string): value is SortableField {
  return (SORTABLE_FIELDS as readonly string[]).includes(value);
}

export interface InquiryFilterRequest {
  searchTerm?: string;
  statusId?: number;
  priorityId?: number;
  organizationName?: string;
  sortBy: SortableField;
  sortDescending: boolean;
  pageNumber: number;
  pageSize: number;
}

export const DEFAULT_FILTER: InquiryFilterRequest = {
  sortBy: 'CreatedAt',
  sortDescending: true,
  pageNumber: 1,
  pageSize: 20,
};
