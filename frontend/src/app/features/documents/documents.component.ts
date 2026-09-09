import { CommonModule } from '@angular/common';
import {
  Component,
  OnDestroy,
  OnInit,
  effect,
  inject,
  signal
} from '@angular/core';
import { DocumentDto } from '../../core/models/document.model';
import { AppStateService } from '../../core/services/app-state.service';
import { DocumentService } from '../../core/services/document.service';

@Component({
  selector: 'app-documents',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="documents">
      <div class="documents__head">
        <h2>Documents</h2>
        <span class="count">{{ state.documents().length }}</span>
      </div>

      <label
        class="dropzone"
        [class.dropzone--active]="dragging()"
        (dragover)="onDragOver($event)"
        (dragleave)="onDragLeave($event)"
        (drop)="onDrop($event)"
      >
        <input type="file" accept="application/pdf,.pdf" hidden (change)="onFileSelected($event)" />
        <div class="dropzone__icon">⬆</div>
        <div class="dropzone__title">Drop a PDF here or click to browse</div>
        <div class="dropzone__hint">Maximum 200&nbsp;MB · PDF only</div>
      </label>

      <div class="upload-status" *ngIf="uploading()">Uploading &amp; queuing…</div>
      <div class="upload-error" *ngIf="uploadError()">{{ uploadError() }}</div>

      <div class="doc-list">
        <button
          type="button"
          class="doc-card"
          *ngFor="let doc of state.documents(); trackBy: trackById"
          [class.doc-card--selected]="doc.id === state.selectedDocumentId()"
          (click)="select(doc)"
        >
          <div class="doc-card__main">
            <div class="doc-card__name" [title]="doc.fileName">{{ doc.fileName }}</div>
            <div class="doc-card__meta">
              {{ formatSize(doc.sizeBytes) }}
              <ng-container *ngIf="doc.status === 'Completed'">
                · {{ doc.pageCount }} pages · {{ doc.chunkCount }} chunks
              </ng-container>
            </div>
            <div class="doc-card__error" *ngIf="doc.status === 'Failed' && doc.errorMessage">
              {{ doc.errorMessage }}
            </div>
          </div>

          <div class="doc-card__side">
            <span class="badge" [ngClass]="badgeClass(doc.status)">
              <span class="badge__dot" *ngIf="isBusy(doc.status)"></span>
              {{ doc.status }}
            </span>
            <span
              class="doc-card__delete"
              title="Delete document"
              (click)="remove($event, doc)"
              >✕</span
            >
          </div>
        </button>

        <div class="empty" *ngIf="!state.loading() && state.documents().length === 0">
          No documents yet. Upload a PDF to get started.
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .documents {
        display: flex;
        flex-direction: column;
        height: 100%;
        padding: 20px;
        gap: 16px;
      }

      .documents__head {
        display: flex;
        align-items: center;
        justify-content: space-between;
      }

      .documents__head h2 {
        font-size: 15px;
        font-weight: 600;
        margin: 0;
        letter-spacing: 0.02em;
      }

      .count {
        font-size: 12px;
        color: var(--text-muted);
        background: var(--surface-hover);
        border-radius: 20px;
        padding: 2px 10px;
      }

      .dropzone {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 6px;
        padding: 26px 16px;
        border: 1.5px dashed var(--border);
        border-radius: var(--radius);
        text-align: center;
        cursor: pointer;
        transition: border-color 0.15s ease, background 0.15s ease;
      }

      .dropzone:hover,
      .dropzone--active {
        border-color: var(--accent);
        background: rgba(91, 141, 239, 0.08);
      }

      .dropzone__icon {
        font-size: 22px;
        color: var(--accent);
      }

      .dropzone__title {
        font-size: 13px;
        font-weight: 500;
      }

      .dropzone__hint {
        font-size: 11px;
        color: var(--text-muted);
      }

      .upload-status {
        font-size: 12px;
        color: var(--accent);
      }

      .upload-error {
        font-size: 12px;
        color: var(--danger);
      }

      .doc-list {
        display: flex;
        flex-direction: column;
        gap: 10px;
        overflow-y: auto;
        flex: 1;
        min-height: 0;
      }

      .doc-card {
        display: flex;
        align-items: flex-start;
        justify-content: space-between;
        gap: 10px;
        text-align: left;
        width: 100%;
        border: 1px solid var(--border);
        background: var(--surface-raised);
        color: var(--text);
        border-radius: 10px;
        padding: 12px 14px;
        transition: border-color 0.15s ease, transform 0.05s ease;
      }

      .doc-card:hover {
        border-color: #33405c;
      }

      .doc-card:active {
        transform: scale(0.997);
      }

      .doc-card--selected {
        border-color: var(--accent);
        box-shadow: 0 0 0 1px var(--accent) inset;
      }

      .doc-card__main {
        min-width: 0;
        flex: 1;
      }

      .doc-card__name {
        font-size: 13px;
        font-weight: 600;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
      }

      .doc-card__meta {
        font-size: 11px;
        color: var(--text-muted);
        margin-top: 3px;
      }

      .doc-card__error {
        font-size: 11px;
        color: var(--danger);
        margin-top: 4px;
        white-space: normal;
      }

      .doc-card__side {
        display: flex;
        flex-direction: column;
        align-items: flex-end;
        gap: 8px;
      }

      .badge {
        display: inline-flex;
        align-items: center;
        gap: 6px;
        font-size: 10.5px;
        font-weight: 600;
        letter-spacing: 0.03em;
        text-transform: uppercase;
        padding: 3px 9px;
        border-radius: 20px;
        white-space: nowrap;
      }

      .badge--pending {
        color: var(--warning);
        background: rgba(224, 163, 78, 0.14);
      }

      .badge--processing {
        color: var(--accent);
        background: rgba(91, 141, 239, 0.16);
      }

      .badge--completed {
        color: var(--success);
        background: rgba(47, 191, 143, 0.16);
      }

      .badge--failed {
        color: var(--danger);
        background: rgba(229, 96, 116, 0.16);
      }

      .badge__dot {
        width: 6px;
        height: 6px;
        border-radius: 50%;
        background: currentColor;
        animation: pulse 1s ease-in-out infinite;
      }

      @keyframes pulse {
        0%,
        100% {
          opacity: 0.35;
        }
        50% {
          opacity: 1;
        }
      }

      .doc-card__delete {
        font-size: 13px;
        color: var(--text-muted);
        padding: 2px 4px;
        border-radius: 6px;
      }

      .doc-card__delete:hover {
        color: var(--danger);
        background: rgba(229, 96, 116, 0.12);
      }

      .empty {
        font-size: 12.5px;
        color: var(--text-muted);
        text-align: center;
        padding: 24px 8px;
      }
    `
  ]
})
export class DocumentsComponent implements OnInit, OnDestroy {
  readonly state = inject(AppStateService);
  private readonly documentService = inject(DocumentService);

  readonly dragging = signal(false);
  readonly uploading = signal(false);
  readonly uploadError = signal<string | null>(null);

  private pollTimer: ReturnType<typeof setInterval> | null = null;

  constructor() {
    // Poll while any document is still being processed; stop when all settle.
    effect(() => {
      const shouldPoll = this.state.hasProcessing();
      if (shouldPoll && this.pollTimer === null) {
        this.pollTimer = setInterval(() => this.state.loadDocuments(), 3000);
      } else if (!shouldPoll && this.pollTimer !== null) {
        clearInterval(this.pollTimer);
        this.pollTimer = null;
      }
    });
  }

  ngOnInit(): void {
    this.state.loadDocuments();
  }

  ngOnDestroy(): void {
    if (this.pollTimer !== null) {
      clearInterval(this.pollTimer);
    }
  }

  trackById(_: number, doc: DocumentDto): string {
    return doc.id;
  }

  select(doc: DocumentDto): void {
    const next = this.state.selectedDocumentId() === doc.id ? null : doc.id;
    this.state.selectDocument(next);
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(true);
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);
    const file = event.dataTransfer?.files?.[0];
    if (file) {
      this.upload(file);
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) {
      this.upload(file);
    }
    input.value = '';
  }

  private upload(file: File): void {
    if (!file.name.toLowerCase().endsWith('.pdf') && file.type !== 'application/pdf') {
      this.uploadError.set('Only PDF files are supported.');
      return;
    }

    this.uploadError.set(null);
    this.uploading.set(true);

    this.documentService.upload(file).subscribe({
      next: () => {
        this.uploading.set(false);
        this.state.loadDocuments();
      },
      error: (err) => {
        this.uploading.set(false);
        this.uploadError.set(err?.error?.error ?? 'Upload failed. Please try again.');
      }
    });
  }

  remove(event: Event, doc: DocumentDto): void {
    event.stopPropagation();
    if (!confirm(`Delete "${doc.fileName}"? This also removes its vectors.`)) {
      return;
    }

    this.documentService.delete(doc.id).subscribe({
      next: () => {
        if (this.state.selectedDocumentId() === doc.id) {
          this.state.selectDocument(null);
        }
        this.state.loadDocuments();
      }
    });
  }

  isBusy(status: DocumentDto['status']): boolean {
    return status === 'Pending' || status === 'Processing';
  }

  badgeClass(status: DocumentDto['status']): string {
    return `badge--${status.toLowerCase()}`;
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }
}
