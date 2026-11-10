import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Api, errorMessage } from '../core/api';
import { Auth } from '../core/auth';
import { Agent, Category, Page, Priority, Ticket, statusLabels } from '../core/models';
import { Icon } from '../shared/icon';
import { StatusBadge } from '../shared/status';

@Component({ imports: [FormsModule, RouterLink, DatePipe, Icon, StatusBadge], template: `
  <div class="page-heading"><div><p class="eyebrow">TICKET MANAGEMENT</p><h1>{{ queue ? 'Antrian tiket' : auth.staff() ? 'Semua tiket' : 'Tiket saya' }} <span class="count-pill">{{ data().total }}</span></h1><p>{{ queue ? 'Temukan permintaan yang belum ditugaskan dan mulai membantu.' : 'Setiap permintaan, tercatat dan terpantau.' }}</p></div><a routerLink="/tickets/new" class="button primary"><hd-icon name="plus" />Buat tiket</a></div>
  <section class="card"><form class="filter-bar" (ngSubmit)="search()"><div class="search-field"><hd-icon name="search" /><input name="search" [(ngModel)]="query" placeholder="Cari judul atau nomor tiket…" aria-label="Cari tiket"></div><select name="status" [(ngModel)]="status" aria-label="Filter status"><option value="">Semua status</option>@for (item of statuses; track item) { <option [value]="item">{{ labels[item] }}</option> }</select><select name="priority" [(ngModel)]="priorityId" aria-label="Filter prioritas"><option value="">Semua prioritas</option>@for (p of priorities(); track p.id) { <option [value]="p.id">{{ p.name }}</option> }</select><button class="button secondary" type="submit">Terapkan</button></form>
    <details class="advanced-filters"><summary>Filter lanjutan & urutan</summary><div class="filter-grid"><label>Kategori<select [(ngModel)]="categoryId"><option value="">Semua kategori</option>@for (c of categories(); track c.id) { <option [value]="c.id">{{ c.name }}</option> }</select></label>@if (auth.staff() && !queue) { <label>Agen<select [(ngModel)]="assigneeId"><option value="">Semua agen</option>@for (a of agents(); track a.id) { <option [value]="a.id">{{ a.fullName }}</option> }</select></label> }<label>Dari tanggal<input type="date" [(ngModel)]="from"></label><label>Sampai tanggal<input type="date" [(ngModel)]="to"></label><label>Urutkan<select [(ngModel)]="sortBy"><option value="createdAt">Tanggal dibuat</option><option value="priority">Prioritas</option><option value="title">Judul</option><option value="dueDate">Batas SLA</option></select></label><label>Arah<select [(ngModel)]="sortDir"><option value="desc">Menurun</option><option value="asc">Menaik</option></select></label></div><button class="text-button" (click)="reset()">Reset filter</button></details>
    @if (error()) { <div class="error" role="alert">{{ error() }} <button class="text-button" (click)="load()">Coba lagi</button></div> }
    @if (loading()) { <div class="loading" role="status">Memuat tiket…</div> } @else {
      <div class="table-wrap"><table><thead><tr><th>Tiket</th><th>Status</th><th>Prioritas</th><th>{{ auth.staff() ? 'Penanggung jawab' : 'Kategori' }}</th><th>Dibuat</th><th aria-label="Buka detail"></th></tr></thead><tbody>
      @for (t of data().items; track t.id) { <tr><td class="ticket-title"><a [routerLink]="['/tickets', t.id]"><small>{{ t.ticketNumber }}</small><strong>{{ t.title }}</strong></a></td><td><hd-status [value]="t.status" /></td><td><span [class]="'priority priority-' + t.priority"><i></i>{{ t.priority }}</span></td><td>{{ auth.staff() ? t.assigneeName || 'Belum ditugaskan' : t.category }}</td><td class="nowrap">{{ t.createdAt | date:'dd MMM yyyy':'Asia/Jakarta' }}</td><td><a class="icon-button" [routerLink]="['/tickets', t.id]" [attr.aria-label]="'Buka ' + t.ticketNumber"><hd-icon name="arrow" /></a></td></tr> }
      </tbody></table></div>
      @if (!data().items.length) { <div class="empty"><span class="empty-icon"><hd-icon name="search" /></span><h3>{{ queue ? 'Antrian sudah tertangani' : 'Tidak ada tiket ditemukan' }}</h3><p>Coba sesuaikan filter atau buat permintaan baru.</p></div> }
    }
    <div class="pagination"><span>{{ data().total }} tiket · Halaman {{ page }} dari {{ pages }}</span><div><button class="button secondary compact" [disabled]="page <= 1 || loading()" (click)="change(-1)">Sebelumnya</button><button class="button secondary compact" [disabled]="page >= pages || loading()" (click)="change(1)">Berikutnya</button></div></div>
  </section>` })
export class TicketsComponent {
  private api = inject(Api); auth = inject(Auth); queue = inject(ActivatedRoute).snapshot.routeConfig?.path === 'queue';
  data = signal<Page<Ticket>>({ items: [], total: 0, page: 1, pageSize: 10 }); categories = signal<Category[]>([]); priorities = signal<Priority[]>([]); agents = signal<Agent[]>([]);
  error = signal(''); loading = signal(false); query = ''; status = ''; priorityId = ''; categoryId = ''; assigneeId = ''; from = ''; to = ''; sortBy = 'createdAt'; sortDir = 'desc'; page = 1;
  labels = statusLabels; statuses = Object.keys(statusLabels) as (keyof typeof statusLabels)[];
  get pages() { return Math.max(1, Math.ceil(this.data().total / 10)); }
  constructor() { this.api.get<Category[]>('categories').subscribe({ next: data => this.categories.set(data), error: e => this.error.set(errorMessage(e)) }); this.api.get<Priority[]>('priorities').subscribe({ next: data => this.priorities.set(data), error: e => this.error.set(errorMessage(e)) }); if (this.auth.staff()) this.api.get<Agent[]>('agents').subscribe({ next: data => this.agents.set(data), error: e => this.error.set(errorMessage(e)) }); this.load(); }
  search() { this.page = 1; this.load(); }
  change(delta: number) { this.page += delta; this.load(); }
  reset() { this.query = this.status = this.priorityId = this.categoryId = this.assigneeId = this.from = this.to = ''; this.sortBy = 'createdAt'; this.sortDir = 'desc'; this.search(); }
  load() {
    this.loading.set(true); this.error.set('');
    const params: Record<string, string | number | boolean> = { page: this.page, pageSize: 10, sortBy: this.sortBy, sortDir: this.sortDir, unassigned: this.queue };
    for (const [key, value] of Object.entries({ search: this.query, status: this.status, priorityId: this.priorityId, categoryId: this.categoryId, assigneeId: this.assigneeId, from: this.from ? new Date(this.from + 'T00:00:00+07:00').toISOString() : '', to: this.to ? new Date(this.to + 'T23:59:59.999+07:00').toISOString() : '' })) if (value) params[key] = value;
    this.api.get<Page<Ticket>>('tickets', params).subscribe({ next: data => { this.data.set(data); this.loading.set(false); }, error: error => { this.error.set(errorMessage(error)); this.loading.set(false); } });
  }
}
