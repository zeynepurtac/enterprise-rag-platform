import { CommonModule } from '@angular/common';
import {
  AfterViewChecked,
  Component,
  ElementRef,
  ViewChild,
  computed,
  inject,
  signal
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  ChatHistoryItem,
  ChatMessage,
  ChatRequest,
  Citation
} from '../../core/models/chat.model';
import { AppStateService } from '../../core/services/app-state.service';
import { ChatService } from '../../core/services/chat.service';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="chat">
      <div class="chat__scope">
        <ng-container *ngIf="state.selectedDocument() as doc; else allScope">
          <span class="scope-label">Chatting with</span>
          <span class="scope-value">{{ doc.fileName }}</span>
          <button class="scope-clear" (click)="state.selectDocument(null)">Use all documents</button>
        </ng-container>
        <ng-template #allScope>
          <span class="scope-label">Chatting with</span>
          <span class="scope-value">All documents</span>
        </ng-template>
      </div>

      <div class="chat__messages" #scrollArea>
        <div class="welcome" *ngIf="messages().length === 0">
          <div class="welcome__icon">💬</div>
          <h3>Ask anything about your documents</h3>
          <p>
            Answers are grounded in your uploaded PDFs and returned with page-level
            citations you can verify.
          </p>
        </div>

        <div
          class="msg"
          *ngFor="let msg of messages()"
          [class.msg--user]="msg.role === 'user'"
          [class.msg--assistant]="msg.role === 'assistant'"
        >
          <div class="msg__avatar">{{ msg.role === 'user' ? 'You' : 'AI' }}</div>
          <div class="msg__body">
            <div class="msg__text">
              {{ msg.content }}<span class="cursor" *ngIf="msg.streaming">▍</span>
            </div>

            <div class="citations" *ngIf="msg.citations?.length">
              <div class="citations__title">Sources</div>
              <div class="citation" *ngFor="let c of msg.citations; let i = index">
                <div class="citation__head">
                  <span class="citation__index">[{{ i + 1 }}]</span>
                  <span class="citation__file">{{ c.fileName }}</span>
                  <span class="citation__page">p.{{ c.pageNumber }}</span>
                  <span class="citation__score">{{ (c.score * 100) | number: '1.0-0' }}%</span>
                </div>
                <div class="citation__snippet">{{ c.snippet }}</div>
              </div>
            </div>
          </div>
        </div>

        <div class="error" *ngIf="error()">{{ error() }}</div>
      </div>

      <div class="chat__composer">
        <textarea
          rows="1"
          placeholder="Ask a question…  (Enter to send, Shift+Enter for a new line)"
          [(ngModel)]="draft"
          [disabled]="busy()"
          (keydown)="onKeydown($event)"
        ></textarea>
        <button class="send" [disabled]="busy() || !draft.trim()" (click)="send()">
          {{ busy() ? '…' : 'Send' }}
        </button>
      </div>
    </div>
  `,
  styles: [
    `
      .chat {
        display: flex;
        flex-direction: column;
        height: 100%;
      }

      .chat__scope {
        display: flex;
        align-items: center;
        gap: 10px;
        padding: 12px 22px;
        border-bottom: 1px solid var(--border);
        font-size: 12.5px;
      }

      .scope-label {
        color: var(--text-muted);
      }

      .scope-value {
        font-weight: 600;
      }

      .scope-clear {
        margin-left: auto;
        font-size: 12px;
        color: var(--accent);
        background: transparent;
        border: 1px solid var(--border);
        border-radius: 8px;
        padding: 5px 10px;
      }

      .scope-clear:hover {
        background: var(--surface-hover);
      }

      .chat__messages {
        flex: 1;
        overflow-y: auto;
        padding: 24px;
        display: flex;
        flex-direction: column;
        gap: 20px;
      }

      .welcome {
        margin: auto;
        text-align: center;
        max-width: 420px;
        color: var(--text-muted);
      }

      .welcome__icon {
        font-size: 34px;
      }

      .welcome h3 {
        color: var(--text);
        margin: 12px 0 8px;
        font-size: 17px;
      }

      .welcome p {
        font-size: 13px;
        line-height: 1.6;
      }

      .msg {
        display: flex;
        gap: 12px;
        max-width: 820px;
      }

      .msg--user {
        align-self: flex-end;
        flex-direction: row-reverse;
      }

      .msg__avatar {
        flex-shrink: 0;
        width: 34px;
        height: 34px;
        border-radius: 9px;
        display: grid;
        place-items: center;
        font-size: 11px;
        font-weight: 700;
      }

      .msg--user .msg__avatar {
        background: var(--accent);
        color: #fff;
      }

      .msg--assistant .msg__avatar {
        background: var(--surface-hover);
        color: var(--accent);
      }

      .msg__body {
        background: var(--surface-raised);
        border: 1px solid var(--border);
        border-radius: 12px;
        padding: 12px 15px;
      }

      .msg--user .msg__body {
        background: rgba(91, 141, 239, 0.14);
        border-color: rgba(91, 141, 239, 0.3);
      }

      .msg__text {
        font-size: 14px;
        line-height: 1.65;
        white-space: pre-wrap;
        word-break: break-word;
      }

      .cursor {
        color: var(--accent);
        animation: blink 1s steps(2) infinite;
      }

      @keyframes blink {
        50% {
          opacity: 0;
        }
      }

      .citations {
        margin-top: 12px;
        border-top: 1px solid var(--border);
        padding-top: 10px;
        display: flex;
        flex-direction: column;
        gap: 8px;
      }

      .citations__title {
        font-size: 10.5px;
        letter-spacing: 0.06em;
        text-transform: uppercase;
        color: var(--text-muted);
      }

      .citation {
        background: var(--surface);
        border: 1px solid var(--border);
        border-radius: 8px;
        padding: 8px 10px;
      }

      .citation__head {
        display: flex;
        align-items: center;
        gap: 8px;
        font-size: 11.5px;
        margin-bottom: 4px;
      }

      .citation__index {
        color: var(--accent);
        font-weight: 700;
      }

      .citation__file {
        font-weight: 600;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
      }

      .citation__page {
        color: var(--text-muted);
      }

      .citation__score {
        margin-left: auto;
        color: var(--success);
        font-weight: 600;
      }

      .citation__snippet {
        font-size: 12px;
        line-height: 1.5;
        color: var(--text-muted);
      }

      .error {
        color: var(--danger);
        font-size: 13px;
        text-align: center;
      }

      .chat__composer {
        display: flex;
        gap: 10px;
        padding: 16px 22px;
        border-top: 1px solid var(--border);
        background: var(--surface);
      }

      textarea {
        flex: 1;
        resize: none;
        max-height: 160px;
        background: var(--surface-raised);
        color: var(--text);
        border: 1px solid var(--border);
        border-radius: 10px;
        padding: 12px 14px;
        font-family: inherit;
        font-size: 14px;
        line-height: 1.5;
        outline: none;
      }

      textarea:focus {
        border-color: var(--accent);
      }

      .send {
        align-self: flex-end;
        background: var(--accent);
        color: #fff;
        border: none;
        border-radius: 10px;
        padding: 12px 22px;
        font-weight: 600;
        font-size: 14px;
        transition: opacity 0.15s ease;
      }

      .send:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    `
  ]
})
export class ChatComponent implements AfterViewChecked {
  readonly state = inject(AppStateService);
  private readonly chatService = inject(ChatService);

  @ViewChild('scrollArea') private scrollArea?: ElementRef<HTMLDivElement>;

  readonly messages = signal<ChatMessage[]>([]);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  draft = '';
  private shouldScroll = false;

  readonly history = computed<ChatHistoryItem[]>(() =>
    this.messages()
      .filter((m) => !m.streaming && m.content.trim().length > 0)
      .map((m) => ({ role: m.role, content: m.content }))
  );

  ngAfterViewChecked(): void {
    if (this.shouldScroll && this.scrollArea) {
      this.scrollArea.nativeElement.scrollTop = this.scrollArea.nativeElement.scrollHeight;
      this.shouldScroll = false;
    }
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
  }

  send(): void {
    const question = this.draft.trim();
    if (!question || this.busy()) {
      return;
    }

    this.error.set(null);
    this.draft = '';

    const priorHistory = this.history();

    this.appendMessage({ role: 'user', content: question });
    const assistant: ChatMessage = { role: 'assistant', content: '', streaming: true };
    this.appendMessage(assistant);

    this.busy.set(true);

    const request: ChatRequest = {
      question,
      documentId: this.state.selectedDocumentId(),
      history: priorHistory
    };

    this.chatService.stream(request).subscribe({
      next: (event) => {
        if (event.type === 'citations' && event.citations) {
          this.updateAssistant((m) => (m.citations = event.citations as Citation[]));
        } else if (event.type === 'token' && event.token) {
          this.updateAssistant((m) => (m.content += event.token));
          this.shouldScroll = true;
        } else if (event.type === 'error') {
          this.error.set(event.message ?? 'The assistant returned an error.');
        }
      },
      error: (err) => {
        this.updateAssistant((m) => (m.streaming = false));
        this.busy.set(false);
        this.error.set(
          typeof err?.message === 'string' ? err.message : 'Failed to reach the assistant.'
        );
      },
      complete: () => {
        this.updateAssistant((m) => (m.streaming = false));
        this.busy.set(false);
        this.shouldScroll = true;
      }
    });
  }

  private appendMessage(message: ChatMessage): void {
    this.messages.update((list) => [...list, message]);
    this.shouldScroll = true;
  }

  private updateAssistant(mutate: (message: ChatMessage) => void): void {
    this.messages.update((list) => {
      const copy = [...list];
      const last = copy[copy.length - 1];
      if (last && last.role === 'assistant') {
        const updated = { ...last };
        mutate(updated);
        copy[copy.length - 1] = updated;
      }
      return copy;
    });
  }
}
