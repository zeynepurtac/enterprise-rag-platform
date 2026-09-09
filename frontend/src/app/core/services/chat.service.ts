import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ChatRequest, ChatResponse, RagStreamEvent } from '../models/chat.model';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/chat`;

  /** Buffered (non-streaming) answer. */
  ask(request: ChatRequest): Observable<ChatResponse> {
    return this.http.post<ChatResponse>(this.baseUrl, request);
  }

  /**
   * Streaming answer via Server-Sent Events. Uses the Fetch API so we can read
   * the response body incrementally, which Angular's HttpClient does not expose
   * conveniently. Emits each decoded {@link RagStreamEvent}.
   */
  stream(request: ChatRequest): Observable<RagStreamEvent> {
    return new Observable<RagStreamEvent>((subscriber) => {
      const controller = new AbortController();

      (async () => {
        try {
          const response = await fetch(`${this.baseUrl}/stream`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(request),
            signal: controller.signal
          });

          if (!response.ok || !response.body) {
            throw new Error(`Stream request failed with status ${response.status}`);
          }

          const reader = response.body.getReader();
          const decoder = new TextDecoder();
          let buffer = '';

          // Server-Sent Events are delimited by a blank line ("\n\n").
          // eslint-disable-next-line no-constant-condition
          while (true) {
            const { value, done } = await reader.read();
            if (done) {
              break;
            }

            buffer += decoder.decode(value, { stream: true });
            const frames = buffer.split('\n\n');
            buffer = frames.pop() ?? '';

            for (const frame of frames) {
              const line = frame.trim();
              if (!line.startsWith('data:')) {
                continue;
              }

              const payload = line.slice('data:'.length).trim();
              if (!payload) {
                continue;
              }

              try {
                const event = JSON.parse(payload) as RagStreamEvent;
                subscriber.next(event);
              } catch {
                // Ignore malformed frames.
              }
            }
          }

          subscriber.complete();
        } catch (error) {
          if (!controller.signal.aborted) {
            subscriber.error(error);
          }
        }
      })();

      return () => controller.abort();
    });
  }
}
