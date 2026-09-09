export interface Citation {
  chunkId: string;
  documentId: string;
  fileName: string;
  pageNumber: number;
  score: number;
  snippet: string;
}

export interface ChatResponse {
  answer: string;
  citations: Citation[];
}

export interface ChatHistoryItem {
  role: 'user' | 'assistant';
  content: string;
}

export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  citations?: Citation[];
  streaming?: boolean;
}

export interface ChatRequest {
  question: string;
  documentId?: string | null;
  history: ChatHistoryItem[];
}

// Event shape emitted by the /chat/stream Server-Sent-Events endpoint.
export interface RagStreamEvent {
  type: 'citations' | 'token' | 'done' | 'error';
  token?: string;
  citations?: Citation[];
  message?: string;
}
