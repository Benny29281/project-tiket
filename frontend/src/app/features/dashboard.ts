import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { Api, errorMessage } from '../core/api';
import { Auth } from '../core/auth';
import { Page, Summary, Ticket, statusLabels } from '../core/models';
import { Icon } from '../shared/icon';
import { StatusBadge } from '../shared/status';

@Component({ imports: [RouterLink, DatePipe, Icon, StatusBadge], template: `
  <div class="page-heading"><div><p class="eyebrow">OVERVIEW</p><h1>Dashboard</h1><p>Pantau permintaan, prioritaskan yang penting.</p></div><a class="button primary" routerLink="/tickets/new"><hd-icon name="plus" />Buat tiket</a></div>
  <section class="welcome-banner"><div><span class="banner-label"><span class="live-dot"></span> WORKSPACE ANDA</span><h2>Halo, {{ auth.user()?.fullName?.split(' ')?.[0] }} <span class="wave">✦</span></h2><p>{{ auth.staff() ? 'Mari buat hari ini lebih produktif. Setiap tiket adalah kesempatan untuk membantu.' : 'Butuh bantuan? Tim IT siap membantu kendala pekerjaan Anda.' }}</p><a routerLink="/tickets">Lihat semua tiket <hd-icon name="arrow" /></a></div><div class="banner-art" aria-hidden="true"><div class="art-orbit"></div><div class="art-ticket"><hd-icon name="ticket" /><span></span><span></span><i><hd-icon name="check" /></i></div><div class="art-dot d1"></div><div class="art-dot d2"></div></div></section>
  @if (error()) { <div class="error" role="alert">{{ error() }} <button class="text-button" (click)="load()">Coba lagi</button></div> }
  @if (loading()) { <div class="loading" role="status">Memuat ringkasan workspace…</div> }
  @if (summary(); as s) {
    <section class="stats-grid" aria-label="Ringkasan tiket">
      <article class="stat-card"><div class="stat-top"><span>Total tiket</span><span class="stat-icon violet"><hd-icon /></span></div><strong>{{ s.total }}</strong><small>Seluruh permintaan bantuan</small></article>
      <article class="stat-card"><div class="stat-top"><span>Tiket aktif</span><span class="stat-icon blue"><hd-icon name="clock" /></span></div><strong>{{ s.active }}</strong><small>Menunggu dan sedang ditangani</small></article>
      <article class="stat-card"><div class="stat-top"><span>Terselesaikan</span><span class="stat-icon green"><hd-icon name="check" /></span></div><strong>{{ s.resolved }}</strong><small>Solusi sudah diberikan</small></article>
      <article class="stat-card"><div class="stat-top"><span>Melewati SLA</span><span class="stat-icon orange"><hd-icon name="alert" /></span></div><strong>{{ s.overdue }}</strong><small>Perlu perhatian lebih</small></article>
    </section>
    <div class="dashboard-columns"><section class="card recent-card"><div class="card-heading"><div><h2>Tiket terbaru</h2><p>Perkembangan permintaan terkini</p></div><a routerLink="/tickets" class="text-link">Lihat semua <hd-icon name="arrow" /></a></div>
      @if (!tickets().length) { <div class="empty"><span class="empty-icon"><hd-icon /></span><h3>Belum ada tiket</h3><p>Permintaan bantuan Anda akan muncul di sini.</p><a routerLink="/tickets/new" class="button secondary">Buat tiket pertama</a></div> }
      @for (t of tickets(); track t.id) { <a class="recent-row" [routerLink]="['/tickets', t.id]"><span class="ticket-square"><hd-icon /></span><div class="recent-content"><small>{{ t.ticketNumber }} <span>· {{ t.category }}</span></small><strong>{{ t.title }}</strong><span>{{ t.requesterName }} <b>·</b> {{ t.createdAt | date:'dd MMM, HH:mm':'Asia/Jakarta' }}</span></div><hd-status [value]="t.status" /><hd-icon name="arrow" /></a> }
    </section><div class="dashboard-side"><section class="card"><div class="card-heading"><div><h2>Distribusi status</h2><p>Gambaran tiket Anda</p></div><hd-icon name="chart" /></div><div class="status-summary"><strong>{{ s.total }}</strong><span>total tiket</span></div><div class="stacked-bar">@for (item of s.byStatus; track item.status) { <span [class]="'segment status-' + item.status" [style.width.%]="s.total ? item.count / s.total * 100 : 0" [title]="labels[item.status] + ': ' + item.count"></span> }</div><div class="status-legend">@for (item of s.byStatus; track item.status) { <div><hd-status [value]="item.status" /><strong>{{ item.count }}</strong></div> } @empty { <p class="muted">Distribusi tersedia setelah tiket dibuat.</p> }</div></section>
      <section class="tip-card"><span class="tip-label"><hd-icon name="headset" /> DUKUNGAN LEBIH TERARAH</span><h3>Mulai dari yang paling penting.</h3><p>{{ auth.staff() ? s.unassigned + ' tiket belum memiliki penanggung jawab. Ambil tiket untuk mulai membantu.' : 'Jelaskan kendala secara rinci agar tim dapat membantu lebih cepat.' }}</p><a [routerLink]="auth.staff() ? '/queue' : '/tickets/new'">{{ auth.staff() ? 'Buka antrian' : 'Buat permintaan' }} <hd-icon name="arrow" /></a></section></div></div>
  }
` })
export class DashboardComponent {
  api = inject(Api); auth = inject(Auth); summary = signal<Summary | null>(null); tickets = signal<Ticket[]>([]);
  error = signal(''); loading = signal(false); labels = statusLabels;
  constructor() { this.load(); }
  load() { this.loading.set(true); this.error.set(''); forkJoin({ summary: this.api.get<Summary>('dashboard/summary'), tickets: this.api.get<Page<Ticket>>('tickets', { pageSize: 5 }) }).subscribe({ next: data => { this.summary.set(data.summary); this.tickets.set(data.tickets.items); this.loading.set(false); }, error: error => { this.error.set(errorMessage(error)); this.loading.set(false); } }); }
}
