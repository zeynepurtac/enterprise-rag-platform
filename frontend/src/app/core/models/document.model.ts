export type DocumentStatus = 'Pending' | 'Processing' | 'Completed' | 'Failed';

export interface DocumentDto {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  pageCount: number;
  chunkCount: number;
  status: DocumentStatus;
  errorMessage?: string | null;
  createdAt: string;
  processedAt?: string | null;
}
