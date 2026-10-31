import { Routes } from '@angular/router';
import { Home } from './pages/home/home';

export const routes: Routes = [
  { path: '', component: Home, title: 'Sumlers Construction | Custom Decks & Home Remodeling' },
  {
    path: 'services',
    loadComponent: () => import('./pages/services/services').then((m) => m.Services),
    title: 'Services | Sumlers Construction',
  },
  {
    path: 'contact',
    loadComponent: () => import('./pages/contact/contact').then((m) => m.Contact),
    title: 'Free Estimate | Sumlers Construction',
  },
  {
    path: 'admin',
    loadComponent: () => import('./pages/admin/admin').then((m) => m.Admin),
    title: 'Admin | Sumlers Construction',
  },
  { path: '**', redirectTo: '' },
];
