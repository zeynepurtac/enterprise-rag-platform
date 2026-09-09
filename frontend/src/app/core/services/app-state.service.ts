import { Injectable, computed, inject, signal } from '@angular/core';
import { DocumentDto } from '../models/document.model';
import { DocumentService } from './document.service';

/**
 * Small signal-based store shared between the documents and chat panes so they
 * stay in sync (e.g. the chat is scoped to the currently selected document).
 */
@Injectable({ providedIn: 'root' })
export class AppStateService {
  private readonly documentService = inject(DocumentService);

  readonly documents = signal<DocumentDto[]>([]);
  readonly selectedDocumentId = signal<string | null>(null);
  readonly loading = signal<boolean>(false);

  readonly selectedDocument = computed(() => {
    const id = this.selectedDocumentId();
    return this.documents().find((d) => d.id === id) ?? null;
  });

  readonly hasProcessing = computed(() =>
    this.documents().some((d) => d.status === 'Pending' || d.status === 'Processing')
  );

  loadDocuments(): void {
    this.loading.set(true);
    this.documentService.list().subscribe({
      next: (docs) => {
        this.documents.set(docs);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  selectDocument(id: string | null): void {
    this.selectedDocumentId.set(id);
  }
}
