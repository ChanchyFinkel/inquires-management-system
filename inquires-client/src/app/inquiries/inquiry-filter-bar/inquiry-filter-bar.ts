import { Component, OnDestroy, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { toSignal } from '@angular/core/rxjs-interop';
import { Subscription, debounceTime, distinctUntilChanged, map } from 'rxjs';
import { InquiryStateService } from '../services/inquiry-state.service';
import { priorityLabel, statusLabel } from '../models/status-labels';

@Component({
  selector: 'app-inquiry-filter-bar',
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatIconModule, MatButtonModule],
  templateUrl: './inquiry-filter-bar.html',
  styleUrl: './inquiry-filter-bar.scss',
})
export class InquiryFilterBar implements OnDestroy {
  private readonly state = inject(InquiryStateService);
  private readonly subscriptions = new Subscription();

  protected readonly statusLabel = statusLabel;
  protected readonly priorityLabel = priorityLabel;

  protected readonly searchControl = new FormControl('', { nonNullable: true });
  protected readonly statuses = toSignal(this.state.statuses$, { initialValue: [] });
  protected readonly priorities = toSignal(this.state.priorities$, { initialValue: [] });

  // The facade is the source of truth (inquiry-state.service.ts) — these controls only ever
  // reflect it. Without this, "Clear filters" (triggered from inquiry-list's empty state) would
  // reset the query but leave the search box and selects showing stale values.
  protected readonly selectedStatusId = toSignal(
    this.state.filterState$.pipe(map((filter) => filter.statusId ?? null)),
    { initialValue: null },
  );
  protected readonly selectedPriorityId = toSignal(
    this.state.filterState$.pipe(map((filter) => filter.priorityId ?? null)),
    { initialValue: null },
  );

  protected readonly hasActiveFilters = toSignal(
    this.state.filterState$.pipe(
      map((filter) => !!filter.searchTerm || filter.statusId != null || filter.priorityId != null),
    ),
    { initialValue: false },
  );

  constructor() {
    // User input -> facade: debounced + distinct so retyping doesn't flood the server; the
    // facade's switchMap (inquiry-state.service.ts) cancels any in-flight request on its own.
    this.subscriptions.add(
      this.searchControl.valueChanges
        .pipe(debounceTime(300), distinctUntilChanged())
        .subscribe((term) => this.state.setSearchTerm(term)),
    );

    // Facade -> user input: keep the box in sync with external changes (clearFilters()) without
    // re-triggering the debounce pipe above (emitEvent: false).
    this.subscriptions.add(
      this.state.filterState$.subscribe((filter) => {
        const term = filter.searchTerm ?? '';
        if (this.searchControl.value !== term) {
          this.searchControl.setValue(term, { emitEvent: false });
        }
      }),
    );
  }

  protected onStatusChange(statusId: number | null): void {
    this.state.setStatus(statusId ?? undefined);
  }

  protected onPriorityChange(priorityId: number | null): void {
    this.state.setPriority(priorityId ?? undefined);
  }

  protected clearFilters(): void {
    this.state.clearFilters();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
}
