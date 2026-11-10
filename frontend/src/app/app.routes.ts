import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './core/auth';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./features/login').then(m => m.LoginComponent) },
  { path: 'register', loadComponent: () => import('./features/login').then(m => m.LoginComponent) },
  { path: '', canActivate: [authGuard], loadComponent: () => import('./layouts/shell').then(m => m.ShellComponent), children: [
    { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
    { path: 'dashboard', loadComponent: () => import('./features/dashboard').then(m => m.DashboardComponent) },
    { path: 'tickets', loadComponent: () => import('./features/tickets').then(m => m.TicketsComponent) },
    { path: 'queue', canActivate: [roleGuard], data: { roles: ['Agent', 'Admin'] }, loadComponent: () => import('./features/tickets').then(m => m.TicketsComponent) },
    { path: 'tickets/new', loadComponent: () => import('./features/ticket-form').then(m => m.TicketFormComponent) },
    { path: 'tickets/:id/edit', loadComponent: () => import('./features/ticket-form').then(m => m.TicketFormComponent) },
    { path: 'tickets/:id', loadComponent: () => import('./features/ticket-detail').then(m => m.TicketDetailComponent) },
    { path: 'admin/users', canActivate: [roleGuard], data: { roles: ['Admin'] }, loadComponent: () => import('./features/users').then(m => m.UsersComponent) },
    { path: 'admin/categories', canActivate: [roleGuard], data: { roles: ['Admin'] }, loadComponent: () => import('./features/categories').then(m => m.CategoriesComponent) }
  ] },
  { path: '**', redirectTo: 'dashboard' }
];
