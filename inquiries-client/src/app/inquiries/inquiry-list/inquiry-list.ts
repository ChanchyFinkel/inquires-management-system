import { DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { InquiryStatusBadge } from '../inquiry-status-badge/inquiry-status-badge';
import { InquiryStatusMenu } from '../inquiry-status-menu/inquiry-status-menu';
import { InquiryResponse } from '../models/inquiry.model';
import { DEFAULT_FILTER } from '../models/query-params.model';
import { InquiryStateService } from '../services/inquiry-state.service';

const DISPLAYED_COLUMNS = ['Title', 'OrganizationName', 'Status', 'Priority', 'CreatedAt', 'UpdatedAt', 'actions'];
const SKELETON_ROW_COUNT = 5;

@Component({
  selector: 'app-inquiry-list',
  imports: [
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatButtonModule,
    MatIconModule,
    DatePipe,
    InquiryStatusBadge,
    InquiryStatusMenu,
  ],
  templateUrl: './inquiry-list.html',
  styleUrl: './inquiry-list.scss',
})
export class InquiryList {
  private readonly state = inject(InquiryStateService);

  protected readonly displayedColumns = DISPLAYED_COLUMNS;
  protected readonly skeletonRows = Array.from({ length: SKELETON_ROW_COUNT });

  protected readonly viewState = toSignal(this.state.viewState$, {
    initialValue: { status: 'loading' as const, items: [], totalCount: 0, pageNumber: 1, pageSize: DEFAULT_FILTER.pageSize, error: null },
  });

  protected readonly currentSort = toSignal(this.state.filterState$, { initialValue: DEFAULT_FILTER });

  protected onSort(sort: Sort): void {
    if (!sort.direction) return; // Material's third click clears sort — keep the last valid order instead
    this.state.setSort(sort.active, sort.direction === 'desc');
  }

  protected onPage(event: PageEvent): void {
    this.state.setPage(event.pageIndex + 1, event.pageSize);
  }

  protected retry(): void {
    this.state.refresh();
  }

  protected clearFilters(): void {
    this.state.clearFilters();
  }

  protected wasUpdated(inquiry: InquiryResponse): boolean {
    return inquiry.updatedAt !== inquiry.createdAt;
  }
}
