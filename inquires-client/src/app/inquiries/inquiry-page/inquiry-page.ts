import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { InquiryFilterBar } from '../inquiry-filter-bar/inquiry-filter-bar';
import { InquiryList } from '../inquiry-list/inquiry-list';
import { InquirySummary } from '../inquiry-summary/inquiry-summary';

@Component({
  selector: 'app-inquiry-page',
  imports: [InquirySummary, InquiryFilterBar, InquiryList, MatButtonModule, MatIconModule],
  templateUrl: './inquiry-page.html',
  styleUrl: './inquiry-page.scss',
})
export class InquiryPage {}
