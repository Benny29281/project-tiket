import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Api, errorMessage } from '../core/api';
import { Category } from '../core/models';
import { Icon } from '../shared/icon';

@Component({ imports: [ReactiveFormsModule, Icon], template: `
  <div class="page-heading"><div><p class="eyebrow">ADMINISTRASI</p><h1>Kategori tiket</h1><p>Kelompokkan permintaan agar mudah diarahkan.</p></div><button class="button primary" (click)="edit()"><hd-icon name="plus" />Tambah kategori</button></div>
  @if (error()) { <div class="error" role="alert">{{ error() }} <button class="text-button" (click)="load()">Muat ulang</button></div> }@if (notice()) { <div class="success" role="status">{{ notice() }}</div> }
  @if (editor()) { <section class="card form-card inline-editor"><h2>{{ editingId ? 'Edit kategori' : 'Kategori baru' }}</h2><form [formGroup]="form" (ngSubmit)="save()"><div class="two-columns"><label>Nama kategori<input formControlName="name" maxlength="100" placeholder="Nama kategori"></label><label>Deskripsi<input formControlName="description" maxlength="500" placeholder="Penjelasan singkat"></label></div><label class="checkbox-label"><input type="checkbox" formControlName="isActive"> Kategori aktif</label>@if (form.touched && form.invalid) { <p class="error-text">Nama kategori harus terdiri dari 2–100 karakter.</p> }<div class="form-actions"><button type="button" class="button secondary" (click)="editor.set(false)">Batal</button><button class="button primary" [disabled]="busy()">Simpan kategori</button></div></form></section> }
  <section class="category-grid">@for (c of categories(); track c.id) { <article class="card category-card"><div class="category-card-top"><span class="stat-icon violet"><hd-icon name="folder" /></span><span class="badge" [class.inactive]="!c.isActive">{{ c.isActive ? 'Aktif' : 'Nonaktif' }}</span></div><h2>{{ c.name }}</h2><p>{{ c.description || 'Belum ada deskripsi.' }}</p><button class="text-button" (click)="edit(c)">Edit kategori <hd-icon name="arrow" /></button></article> } @empty { <div class="empty"><h3>Belum ada kategori</h3><p>Tambahkan kategori untuk mulai mengelompokkan tiket.</p></div> }</section>
` })
export class CategoriesComponent {
  private api = inject(Api); private fb = inject(FormBuilder); categories = signal<Category[]>([]); error = signal(''); notice = signal(''); editor = signal(false); busy = signal(false); editingId: number | null = null;
  form = this.fb.nonNullable.group({ name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]], description: ['', Validators.maxLength(500)], isActive: [true] });
  constructor() { this.load(); }
  load() { this.api.get<Category[]>('categories').subscribe({ next: d => this.categories.set(d), error: e => this.error.set(errorMessage(e)) }); }
  edit(c?: Category) { this.editingId = c?.id || null; this.form.reset({ name: c?.name || '', description: c?.description || '', isActive: c?.isActive ?? true }); this.editor.set(true); }
  save() { this.form.markAllAsTouched(); if (this.form.invalid || this.busy()) return; this.busy.set(true); this.error.set(''); const request = this.editingId ? this.api.put('categories/' + this.editingId, this.form.getRawValue()) : this.api.post('categories', this.form.getRawValue()); request.subscribe({ next: () => { this.editor.set(false); this.busy.set(false); this.notice.set('Kategori berhasil disimpan.'); this.load(); }, error: e => { this.error.set(errorMessage(e)); this.busy.set(false); } }); }
}
