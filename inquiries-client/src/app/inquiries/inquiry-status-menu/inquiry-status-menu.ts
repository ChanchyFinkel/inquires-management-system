import { Component, Input, ViewChild, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule, MatMenuTrigger } from '@angular/material/menu';
import { MatRadioModule } from '@angular/material/radio';
import { MatSnackBar } from '@angular/material/snack-bar';
import { InquiryResponse } from '../models/inquiry.model';
import { statusLabel } from '../models/status-labels';
import { InquiryStateService } from '../services/inquiry-state.service';

@Component({
  selector: 'app-inquiry-status-menu',
  imports: [MatMenuModule, MatRadioModule, MatButtonModule, FormsModule],
  templateUrl: './inquiry-status-menu.html',
  styleUrl: './inquiry-status-menu.scss',
})
export class InquiryStatusMenu {
  private readonly state = inject(InquiryStateService);
  private readonly snackBar = inject(MatSnackBar);

  @ViewChild(MatMenuTrigger) private menuTrigger?: MatMenuTrigger;

  private readonly inquirySignal = signal<InquiryResponse | null>(null);
  @Input({ required: true })
  set inquiry(value: InquiryResponse) {
    this.inquirySignal.set(value);
    this.pendingStatusId.set(value.statusId);
  }

  protected readonly statuses = toSignal(this.state.statuses$, { initialValue: [] });
  protected readonly statusLabel = statusLabel;
  protected readonly pendingStatusId = signal<number | null>(null);
  protected readonly saving = signal(false);

  protected readonly canSave = computed(() => {
    const inquiry = this.inquirySignal();
    return !!inquiry && this.pendingStatusId() !== inquiry.statusId && !this.saving();
  });

  protected onMenuOpened(): void {
    const inquiry = this.inquirySignal();
    if (inquiry) this.pendingStatusId.set(inquiry.statusId);
  }

  protected cancel(): void {
    const inquiry = this.inquirySignal();
    if (inquiry) this.pendingStatusId.set(inquiry.statusId);
    this.menuTrigger?.closeMenu();
  }

  protected save(): void {
    const inquiry = this.inquirySignal();
    const statusId = this.pendingStatusId();
    if (!inquiry || statusId == null || statusId === inquiry.statusId) return;

    this.saving.set(true);
    this.state.updateStatus(inquiry.inquiryId, statusId).subscribe({
      next: (updated) => {
        this.saving.set(false);
        this.menuTrigger?.closeMenu();
        const label = statusLabel(updated.statusName);
        this.snackBar.open(`הסטטוס עודכן ל"${label}" בהצלחה`, 'סגירה', { duration: 4000 });
      },
      error: () => {
        this.saving.set(false);
        this.menuTrigger?.closeMenu();
        this.snackBar.open('השמירה נכשלה, נסו שוב', 'סגירה', { duration: 5000 });
      },
    });
  }
}
