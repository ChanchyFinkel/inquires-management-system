import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, EMPTY, Observable, catchError, switchMap } from 'rxjs';
import { UiError, toUiError } from '../../core/models/ui-error.model';
import { InquiryResponse, InquirySummary, PriorityResponse, StatusResponse } from '../models/inquiry.model';
import { DEFAULT_FILTER, InquiryFilterRequest, SortableField, isSortableField } from '../models/query-params.model';
import { InquiryService } from './inquiry.service';

export type ViewStatus = 'loading' | 'error' | 'empty' | 'data';

export interface InquiryViewState {
  status: ViewStatus;
  items: InquiryResponse[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  error: UiError | null;
}

const INITIAL_VIEW_STATE: InquiryViewState = {
  status: 'loading',
  items: [],
  totalCount: 0,
  pageNumber: 1,
  pageSize: DEFAULT_FILTER.pageSize,
  error: null,
};

@Injectable({ providedIn: 'root' })
export class InquiryStateService {
  private readonly inquiryService = inject(InquiryService);

  private readonly filterState = new BehaviorSubject<InquiryFilterRequest>({ ...DEFAULT_FILTER });
  readonly filterState$: Observable<InquiryFilterRequest> = this.filterState.asObservable();

  private readonly viewStateSubject = new BehaviorSubject<InquiryViewState>(INITIAL_VIEW_STATE);
  readonly viewState$: Observable<InquiryViewState> = this.viewStateSubject.asObservable();

  private readonly statusesSubject = new BehaviorSubject<StatusResponse[]>([]);
  readonly statuses$: Observable<StatusResponse[]> = this.statusesSubject.asObservable();

  private readonly prioritiesSubject = new BehaviorSubject<PriorityResponse[]>([]);
  readonly priorities$: Observable<PriorityResponse[]> = this.prioritiesSubject.asObservable();

  private readonly summarySubject = new BehaviorSubject<InquirySummary[]>([]);
  readonly summary$: Observable<InquirySummary[]> = this.summarySubject.asObservable();

  constructor() {
    this.inquiryService.getStatuses().subscribe((statuses) => this.statusesSubject.next(statuses));
    this.inquiryService.getPriorities().subscribe((priorities) => this.prioritiesSubject.next(priorities));
    this.loadSummary();

    this.filterState
      .pipe(
        switchMap((filter) => {
          this.patchViewState({ status: 'loading' });
          return this.inquiryService.getInquiries(filter).pipe(
            catchError((err) => {
              this.patchViewState({ status: 'error', error: toUiError(err) });
              return EMPTY;
            }),
          );
        }),
      )
      .subscribe((page) => {
        this.viewStateSubject.next({
          status: page.totalCount === 0 ? 'empty' : 'data',
          items: page.items,
          totalCount: page.totalCount,
          pageNumber: page.pageNumber,
          pageSize: page.pageSize,
          error: null,
        });
      });
  }

  setSearchTerm(term: string): void {
    this.patchFilter({ searchTerm: term || undefined, pageNumber: 1 });
  }

  setStatus(statusId: number | undefined): void {
    this.patchFilter({ statusId, pageNumber: 1 });
  }

  setPriority(priorityId: number | undefined): void {
    this.patchFilter({ priorityId, pageNumber: 1 });
  }

  setSort(sortBy: string, sortDescending: boolean): void {
    if (!isSortableField(sortBy)) return; // client-side mirror of the server whitelist
    this.patchFilter({ sortBy: sortBy as SortableField, sortDescending, pageNumber: 1 });
  }

  setPage(pageNumber: number, pageSize: number): void {
    this.patchFilter({ pageNumber, pageSize });
  }

  clearFilters(): void {
    this.filterState.next({ ...DEFAULT_FILTER });
  }

  refresh(): void {
    this.filterState.next({ ...this.filterState.value });
  }

  updateStatus(id: number, statusId: number): Observable<InquiryResponse> {
    const currentState = this.viewStateSubject.value;
    const previousItems = currentState.items;

    this.patchViewState({
      items: currentState.items.map((item) => (item.inquiryId === id ? { ...item, statusId } : item)),
    });

    return this.inquiryService.updateStatus(id, statusId).pipe(
      switchMap((updated) => {
        this.patchViewState({
          items: this.viewStateSubject.value.items.map((item) => (item.inquiryId === id ? updated : item)),
        });
        this.loadSummary();
        return [updated];
      }),
      catchError((err) => {
        this.patchViewState({ items: previousItems });
        throw toUiError(err);
      }),
    );
  }

  private patchFilter(patch: Partial<InquiryFilterRequest>): void {
    this.filterState.next({ ...this.filterState.value, ...patch });
  }

  private patchViewState(patch: Partial<InquiryViewState>): void {
    this.viewStateSubject.next({ ...this.viewStateSubject.value, ...patch });
  }

  private loadSummary(): void {
    this.inquiryService.getSummary().subscribe((summary) => this.summarySubject.next(summary));
  }
}
