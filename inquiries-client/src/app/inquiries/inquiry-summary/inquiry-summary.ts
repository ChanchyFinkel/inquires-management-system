import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { combineLatest, map } from 'rxjs';
import { InquiryStateService } from '../services/inquiry-state.service';
import { statusLabel, toKebabKey } from '../models/status-labels';

interface SummaryCard {
  statusKey: string;
  label: string;
  count: number;
}

@Component({
  selector: 'app-inquiry-summary',
  imports: [CommonModule],
  templateUrl: './inquiry-summary.html',
  styleUrl: './inquiry-summary.scss',
})
export class InquirySummary {
  private readonly state = inject(InquiryStateService);

  // /summary only returns groups that actually have rows (GroupBy over Inquiries) — a status
  // with zero matches is simply absent from the array, so this fills it back in as 0 rather
  // than silently dropping the card.
  protected readonly cards = toSignal(
    combineLatest([this.state.statuses$, this.state.summary$]).pipe(
      map(([statuses, summary]): SummaryCard[] =>
        statuses.map((status) => ({
          statusKey: toKebabKey(status.name),
          label: statusLabel(status.name),
          count: summary.find((s) => s.statusName === status.name)?.count ?? 0,
        })),
      ),
    ),
    { initialValue: [] as SummaryCard[] },
  );
}
