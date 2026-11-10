import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Auth } from '../core/auth';
import { Icon } from '../shared/icon';

@Component({ imports: [RouterLink, RouterLinkActive, RouterOutlet, Icon], template: `
  <a href="#main" class="skip-link">Lewati ke konten</a>
  <div class="app-shell">
    @if (menu()) { <button class="nav-backdrop" aria-label="Tutup navigasi" (click)="menu.set(false)"></button> }
    <aside class="sidebar" [class.mobile-open]="menu()">
      <a routerLink="/dashboard" class="brand"><span class="brand-icon"><hd-icon name="headset" /></span>HelpDesk<span>Ku</span><small>WORKSPACE</small></a>
      <div class="workspace"><span class="workspace-avatar">IT</span><div><strong>IT Support</strong><small>Ruang kerja tim</small></div><span class="workspace-dot"></span></div>
      <p class="nav-label">RUANG KERJA</p>
      <nav aria-label="Navigasi utama" (click)="menu.set(false)">
        <a routerLink="/dashboard" routerLinkActive="active"><hd-icon name="grid" />Dashboard</a>
        <a routerLink="/tickets" routerLinkActive="active"><hd-icon />{{ auth.staff() ? 'Semua tiket' : 'Tiket saya' }}</a>
        @if (auth.staff()) { <a routerLink="/queue" routerLinkActive="active"><hd-icon name="queue" />Antrian tiket</a> }
        @if (auth.admin()) {
          <p class="nav-label">ADMINISTRASI</p>
          <a routerLink="/admin/users" routerLinkActive="active"><hd-icon name="users" />Pengguna</a>
          <a routerLink="/admin/categories" routerLinkActive="active"><hd-icon name="folder" />Kategori</a>
        }
      </nav>
      <div class="sidebar-bottom"><div class="support-note"><span class="live-dot"></span><strong>Satu tempat. Lebih terarah.</strong><p>Setiap kendala punya jalan keluar.</p></div>
        <div class="profile"><span class="avatar">{{ auth.user()?.fullName?.slice(0, 2)?.toUpperCase() }}</span><div><strong>{{ auth.user()?.fullName }}</strong><small>{{ auth.user()?.role }}</small></div><button class="icon-button" title="Keluar" aria-label="Keluar" (click)="logout()"><hd-icon name="logout" /></button></div>
      </div>
    </aside>
    <div class="main-wrap"><header class="topbar"><div class="topbar-left"><button class="icon-button mobile-toggle" aria-label="Buka navigasi" (click)="menu.set(true)"><hd-icon name="menu" /></button><span>Workspace</span><span class="separator">/</span><strong>IT Helpdesk</strong></div><div class="topbar-right"><span class="environment"><span class="live-dot"></span>Ruang kerja aktif</span><span class="avatar small">{{ auth.user()?.fullName?.slice(0, 2)?.toUpperCase() }}</span></div></header>
      <main id="main" tabindex="-1"><router-outlet /></main><footer>HelpDeskKu <span>Lebih terorganisir. Lebih responsif.</span><span>IT Support Workspace</span></footer>
    </div>
  </div>` })
export class ShellComponent {
  auth = inject(Auth); menu = signal(false);
  logout() { this.auth.logout().subscribe({ error: () => {} }); }
}
