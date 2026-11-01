import { Routes } from '@angular/router';
import { Home } from './pages/home/home';

// Page titles come from SeoService (the admin page edits them), not from the routes.
export const routes: Routes = [
  { path: '', component: Home },
  {
    path: 'services',
    loadComponent: () => import('./pages/services/services').then((m) => m.Services),
  },
  {
    path: 'contact',
    loadComponent: () => import('./pages/contact/contact').then((m) => m.Contact),
  },
  {
    path: 'admin',
    loadComponent: () => import('./pages/admin/admin').then((m) => m.Admin),
  },
  {
    // Each service has its own page, such as /deck-construction. The admin page sets the address.
    path: ':serviceKey',
    loadComponent: () => import('./pages/service/service-page').then((m) => m.ServicePageComponent),
  },
  {
    path: '**',
    loadComponent: () => import('./pages/not-found').then((m) => m.NotFound),
  },
];
