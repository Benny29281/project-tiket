import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { Api, errorMessage } from '../core/api';
import { Auth } from '../core/auth';
import { Agent, Status, TicketDetail, statusLabels } from '../core/models';
import { Icon } from '../shared/icon';
import { StatusBadge } from '../shared/status';

@Component({ imports: [DatePipe, FormsModule, ReactiveFormsModule, RouterLink, Icon, StatusBadge], template: `
  <a routerLink="/tickets" class="back-link"><hd-icon name="back" /> Kembali ke tiket</a>
  @if (error()) { <div class="error" role="alert">{{ error() }}</div> }
  @if (notice()) { <div class="success" role="status">{{ notice() }}</div> }
  @if (loading()) { <div class="loading" role="status">Memuat detail tiket…</div> }
  @if (data(); as d) {
    <div class="page-heading detail-heading"><div><p class="eyebrow">{{ d.ticket.ticketNumber }} <span> / {{ d.ticket.category }}</span></p><h1>{{ d.ticket.title }}</h1><p>Dibuat oleh {{ d.ticket.requesterName }} · {{ d.ticket.createdAt | date:'dd MMM yyyy, HH:mm':'Asia/Jakarta' }} WIB</p></div><hd-status [value]="d.ticket.status" /></div>
    <div class="detail-columns"><div><section class="card content-card"><div class="card-heading"><h2>Deskripsi kendala</h2>@if (auth.staff() || d.ticket.status === 'Open') { <a [routerLink]="['/tickets', id, 'edit']" class="text-link">Edit tiket</a> }</div><p class="preserve-lines">{{ d.ticket.description }}</p></section>
      <section class="card content-card conversation"><div class="card-heading"><div><h2>Percakapan</h2><p>Diskusikan solusi bersama tim</p></div><span class="count-pill">{{ d.comments.length }}</span></div>
        @for (c of d.comments; track c.id) { <article class="comment" [class.internal]="c.isInternal"><span class="avatar">{{ c.authorName.slice(0, 2).toUpperCase() }}</span><div><div class="comment-header"><strong>{{ c.authorName }}</strong>@if (c.isInternal) { <span class="badge">Catatan internal</span> }<time>{{ c.createdAt | date:'dd MMM, HH:mm':'Asia/Jakarta' }} WIB</time></div><p class="preserve-lines">{{ c.content }}</p></div></article> } @empty { <div class="empty compact-empty"><p>Belum ada percakapan. Mulai diskusi di bawah.</p></div> }
        <form [formGroup]="commentForm" (ngSubmit)="comment()" class="comment-form"><label for="comment">Tulis balasan</label><textarea id="comment" formControlName="content" rows="4" maxlength="10000" placeholder="Tulis pesan atau informasi tambahan…"></textarea><div class="form-actions">@if (auth.staff()) { <label class="checkbox-label"><input type="checkbox" formControlName="isInternal"> Catatan internal</label> }<button class="button primary" [disabled]="busy() || commentForm.invalid">Kirim balasan<hd-icon name="arrow" /></button></div></form>
      </section>
      <section class="card content-card"><div class="card-heading"><h2>Riwayat aktivitas</h2></div><ol class="timeline">@for (a of d.activities; track a.id) { <li><i></i><div><strong>{{ a.actorName }}</strong><p>{{ activityLabel(a.activityType) }}@if (a.activityType === 'StatusChanged') { : {{ statusLabel(a.oldValue) }} → {{ statusLabel(a.newValue) }} }</p><small>{{ a.createdAt | date:'dd MMM yyyy, HH:mm':'Asia/Jakarta' }} WIB</small></div></li> }</ol></section>
    </div><aside><section class="card content-card"><h2>Detail permintaan</h2><dl class="detail-info"><div><dt>Prioritas</dt><dd><span [class]="'priority priority-' + d.ticket.priority"><i></i>{{ d.ticket.priority }}</span></dd></div><div><dt>Kategori</dt><dd>{{ d.ticket.category }}</dd></div><div><dt>Penanggung jawab</dt><dd>{{ d.ticket.assigneeName || 'Belum ditugaskan' }}</dd></div><div><dt>Batas penyelesaian</dt><dd [class.overdue]="overdue(d)">{{ d.ticket.dueDate | date:'dd MMM yyyy, HH:mm':'Asia/Jakarta' }} WIB</dd></div><div><dt>Terakhir diperbarui</dt><dd>{{ d.ticket.updatedAt | date:'dd MMM, HH:mm':'Asia/Jakarta' }} WIB</dd></div></dl>
      @if (transitions(d.ticket.status).length) { <label>Ubah status<select [(ngModel)]="nextStatus"><option value="">Pilih status</option>@for (s of transitions(d.ticket.status); track s) { <option [value]="s">{{ labels[s] }}</option> }</select></label><button class="button secondary full" [disabled]="busy() || !nextStatus" (click)="changeStatus()">Simpan status</button> }
      @if (auth.staff()) { <hr><label>Tugaskan agen<select [(ngModel)]="assignee"><option value="">Belum ditugaskan</option>@for (a of agents(); track a.id) { <option [value]="a.id">{{ a.fullName }}</option> }</select></label><button class="button secondary full" [disabled]="busy()" (click)="assign()">Simpan penugasan</button>@if (!d.ticket.assigneeId) { <button class="text-button full take-ticket" [disabled]="busy()" (click)="take()">Ambil tiket ini</button> } }
    </section>@if (auth.admin()) { <section class="danger-zone"><p>Arsipkan tiket dari daftar aktif.</p><button class="text-button danger" [disabled]="busy()" (click)="confirmDelete.set(true)">Arsipkan tiket</button>@if (confirmDelete()) { <div class="delete-confirm" role="alert"><p>Yakin ingin mengarsipkan {{ d.ticket.ticketNumber }}?</p><button class="button danger-button" [disabled]="busy()" (click)="remove()">Ya, arsipkan</button><button class="text-button" (click)="confirmDelete.set(false)">Batal</button></div> }</section> }</aside></div>
  }
` })
export class TicketDetailComponent {
  private api = inject(Api); private router = inject(Router); private fb = inject(FormBuilder); auth = inject(Auth);
  id = Number(inject(ActivatedRoute).snapshot.paramMap.get('id')); data = signal<TicketDetail | null>(null); agents = signal<Agent[]>([]); error = signal(''); notice = signal(''); busy = signal(false); loading = signal(true); confirmDelete = signal(false); nextStatus = ''; assignee = ''; labels = statusLabels;
  commentForm = this.fb.nonNullable.group({ content: ['', [Validators.required, Validators.maxLength(10000)]], isInternal: [false] });
  constructor() { this.load(); if (this.auth.staff()) this.api.get<Agent[]>('agents').subscribe({ next: d => this.agents.set(d), error: e => this.error.set(errorMessage(e)) }); }
  load() { this.api.get<TicketDetail>('tickets/' + this.id).subscribe({ next: d => { this.data.set(d); this.assignee = d.ticket.assigneeId || ''; this.loading.set(false); }, error: e => { this.error.set(errorMessage(e)); this.loading.set(false); } }); }
  overdue(d: TicketDetail) { return !['Resolved', 'Closed'].includes(d.ticket.status) && new Date(d.ticket.dueDate) < new Date(); }
  transitions(status: Status): Status[] { const staff: Record<Status, Status[]> = { Open: ['InProgress'], InProgress: ['Pending', 'Resolved'], Pending: ['InProgress', 'Resolved'], Resolved: ['Closed', 'Reopened'], Closed: ['Reopened'], Reopened: ['InProgress'] }; return this.auth.staff() ? staff[status] : status === 'Resolved' ? ['Closed', 'Reopened'] : status === 'Closed' ? ['Reopened'] : []; }
  statusLabel(value: string | null) { return this.labels[value as Status] || value; }
  activityLabel(type: string) { return ({ Created: 'Membuat tiket', StatusChanged: 'Mengubah status', Assigned: 'Memperbarui penanggung jawab', PriorityChanged: 'Mengubah prioritas', Updated: 'Memperbarui informasi tiket', Deleted: 'Mengarsipkan tiket' } as Record<string, string>)[type] || type; }
  run(request: Observable<unknown>, message: string, done?: () => void) { if (this.busy()) return; this.busy.set(true); this.error.set(''); this.notice.set(''); request.subscribe({ next: () => { this.busy.set(false); this.notice.set(message); done?.(); this.load(); }, error: e => { this.error.set(errorMessage(e)); this.busy.set(false); } }); }
  comment() { if (this.commentForm.invalid || !this.commentForm.controls.content.value.trim()) return; this.run(this.api.post('tickets/' + this.id + '/comments', this.commentForm.getRawValue()), 'Balasan berhasil dikirim.', () => this.commentForm.reset()); }
  changeStatus() { this.run(this.api.patch('tickets/' + this.id + '/status', { status: this.nextStatus }), 'Status berhasil diperbarui.', () => { this.nextStatus = ''; }); }
  assign() { this.run(this.api.patch('tickets/' + this.id + '/assign', { assigneeId: this.assignee || null }), 'Penugasan berhasil diperbarui.'); }
  take() { this.assignee = this.auth.user()!.id; this.assign(); }
  remove() { this.run(this.api.delete('tickets/' + this.id), 'Tiket diarsipkan.', () => { void this.router.navigateByUrl('/tickets'); }); }
}
