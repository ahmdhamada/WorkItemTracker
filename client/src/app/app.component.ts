import { Component, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { catchError, debounceTime, distinctUntilChanged, map, of, startWith, switchMap, tap } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Status, WorkItem, WorkItemService } from './work-item.service';

@Component({ selector: 'app-root', standalone: true, imports: [CommonModule, ReactiveFormsModule], template: `
<main class="shell"><header><div class="brand">WI</div><div><p class="eyebrow">WORKSPACE</p><h1>Work items</h1></div><span class="count">{{total}} items</span></header>
<section class="create panel"><div><p class="eyebrow">ADD TO YOUR LIST</p><h2>Create a work item</h2></div>
<form [formGroup]="form" (ngSubmit)="create()"><label>Title<input formControlName="title" maxlength="120" placeholder="What needs to get done?"></label>
<label>Description <span class="optional">Optional</span><input formControlName="description" placeholder="Add a little context"></label><button class="primary" [disabled]="form.invalid || saving">{{saving ? 'Adding…' : '＋ Add item'}}</button></form>
<p class="form-error" *ngIf="createError">{{createError}}</p></section>
<section class="list-section"><div class="list-heading"><div><p class="eyebrow">YOUR BOARD</p><h2>Keep things moving</h2></div><span class="subtle">{{total}} results</span></div>
<div class="filters"><label class="search">⌕<input [formControl]="search" placeholder="Search work items…"></label><label class="select"><select [formControl]="status"><option value="">All statuses</option><option value="Todo">To do</option><option value="InProgress">In progress</option><option value="Done">Done</option></select></label></div>
<div class="state" *ngIf="loading">Loading your work items…</div><div class="state error" *ngIf="error">{{error}} <button (click)="refresh()">Try again</button></div>
<div class="cards" *ngIf="!loading && !error && items.length"><article class="item panel" *ngFor="let item of items"><div class="item-top"><span class="badge" [class.todo]="item.status==='Todo'" [class.progress]="item.status==='InProgress'" [class.done]="item.status==='Done'">{{label(item.status)}}</span><span class="date">{{item.createdAt | date:'MMM d, y'}}</span></div><h3>{{item.title}}</h3><p class="description" *ngIf="item.description">{{item.description}}</p><div class="item-bottom"><span class="id">WI-{{item.id}}</span><button *ngIf="item.status!=='Done'" class="advance" [disabled]="busyId===item.id" (click)="advance(item)">{{busyId===item.id?'Updating…':item.status==='Todo'?'Start work →':'Mark done →'}}</button><span class="complete" *ngIf="item.status==='Done'">✓ Complete</span></div></article></div>
<div class="state empty" *ngIf="!loading && !error && !items.length"><div class="empty-icon">✳</div><h3>{{search.value || status.value ? 'No matching work items' : 'A clear desk, for now'}}</h3><p>{{search.value || status.value ? 'Try changing your search or filter.' : 'Add your first work item above to get started.'}}</p></div>
<footer *ngIf="total>20 && !loading"><span>Page {{page}} of {{pages}}</span><div><button [disabled]="page<=1" (click)="setPage(page-1)">← Previous</button><button [disabled]="page>=pages" (click)="setPage(page+1)">Next →</button></div></footer></section>
<p class="fineprint">A small step forward is still progress.</p></main>` })
export class AppComponent {
  private api = inject(WorkItemService); private destroyRef = inject(DestroyRef); private fb = inject(FormBuilder);
  form = this.fb.group({ title: ['', [Validators.required, Validators.maxLength(120)]], description: [''] });
  search = this.fb.control(''); status = this.fb.control(''); items: WorkItem[] = []; total = 0; page = 1; pages = 1;
  loading = true; saving = false; busyId: number | null = null; error = ''; createError = '';
  constructor() { this.search.valueChanges.pipe(startWith(''), debounceTime(250), distinctUntilChanged(), switchMap(() => { this.page=1; return this.fetch(); }), takeUntilDestroyed(this.destroyRef)).subscribe();
    this.status.valueChanges.pipe(startWith(''), distinctUntilChanged(), switchMap(() => { this.page=1; return this.fetch(); }), takeUntilDestroyed(this.destroyRef)).subscribe(); }
  private fetch() { this.loading=true; this.error=''; return this.api.list(this.search.value || '', this.status.value || '', this.page).pipe(tap(result => { this.items=result.items; this.total=result.totalCount; this.pages=Math.max(1,Math.ceil(result.totalCount/result.pageSize)); this.loading=false; }), catchError(() => { this.items=[]; this.total=0; this.loading=false; this.error='Could not load work items. Check that the API is running and try again.'; return of(null); })); }
  refresh() { this.fetch().subscribe(); }
  setPage(page: number) { this.page=page; this.fetch().subscribe(); }
  create() { if (this.form.invalid) return; this.saving=true; this.createError=''; this.api.create(this.form.value.title!.trim(),this.form.value.description || '').pipe(switchMap(() => { this.form.reset(); return this.fetch(); }), catchError(() => { this.createError='Could not add this item. Please try again.'; return of(null); })).subscribe(() => this.saving=false); }
  advance(item: WorkItem) { this.busyId=item.id; this.api.advance(item).pipe(switchMap(() => this.fetch()), catchError(() => { this.error='Could not update the status. Please try again.'; this.loading=false; return of(null); })).subscribe(() => this.busyId=null); }
  label(status: Status) { return status==='Todo'?'To do':status==='InProgress'?'In progress':'Done'; }
}
