import { DecimalPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { combineLatest, map } from 'rxjs';
import { statusLabel, toKebabKey } from '../inquiries/models/status-labels';
import { InquiryStateService } from '../inquiries/services/inquiry-state.service';

interface StatusSlice {
  statusKey: string;
  label: string;
  count: number;
  percent: number;
  startDeg: number;
  endDeg: number;
}

@Component({
  selector: 'app-dashboard',
  imports: [DecimalPipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  private readonly state = inject(InquiryStateService);

  protected readonly slices = toSignal(
    combineLatest([this.state.statuses$, this.state.summary$]).pipe(
      map(([statuses, summary]): StatusSlice[] => {
        const counts = statuses.map((status) => ({
          statusKey: toKebabKey(status.name),
          label: statusLabel(status.name),
          count: summary.find((s) => s.statusName === status.name)?.count ?? 0,
        }));
        const total = counts.reduce((sum, c) => sum + c.count, 0) || 1;

        let cursor = 0;
        return counts.map((c) => {
          const percent = c.count / total;
          const startDeg = cursor * 360;
          cursor += percent;
          return { ...c, percent, startDeg, endDeg: cursor * 360 };
        });
      }),
    ),
    { initialValue: [] as StatusSlice[] },
  );

  protected readonly total = toSignal(
    this.state.summary$.pipe(map((summary) => summary.reduce((sum, s) => sum + s.count, 0))),
    { initialValue: 0 },
  );

  protected readonly maxCount = toSignal(
    this.state.summary$.pipe(map((summary) => Math.max(1, ...summary.map((s) => s.count)))),
    { initialValue: 1 },
  );

  protected conicGradient(slices: StatusSlice[]): string {
    const stops = slices.map((s) => `var(--dashboard-${s.statusKey}) ${s.startDeg}deg ${s.endDeg}deg`);
    return `conic-gradient(${stops.join(', ')})`;
  }
}
