import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'inquiries', pathMatch: 'full' },
  {
    path: 'inquiries',
    loadComponent: () => import('./inquiries/inquiry-page/inquiry-page').then((m) => m.InquiryPage),
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./dashboard/dashboard').then((m) => m.Dashboard),
  },
  { path: '**', redirectTo: 'inquiries' },
];
