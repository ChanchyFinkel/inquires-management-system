import { Component, Input, computed, signal } from '@angular/core';
import { priorityLabel, statusLabel, toKebabKey } from '../models/status-labels';

export type BadgeKind = 'status' | 'priority';

@Component({
  selector: 'app-inquiry-status-badge',
  templateUrl: './inquiry-status-badge.html',
  styleUrl: './inquiry-status-badge.scss',
})
export class InquiryStatusBadge {
  private readonly kindSignal = signal<BadgeKind>('status');
  private readonly valueSignal = signal<string>('');

  @Input({ required: true })
  set kind(value: BadgeKind) {
    this.kindSignal.set(value);
  }

  @Input({ required: true })
  set value(value: string) {
    this.valueSignal.set(value);
  }

  protected readonly label = computed(() =>
    this.kindSignal() === 'status' ? statusLabel(this.valueSignal()) : priorityLabel(this.valueSignal()),
  );

  protected readonly cssClass = computed(
    () => `badge badge--${this.kindSignal()}-${toKebabKey(this.valueSignal())}`,
  );
}
