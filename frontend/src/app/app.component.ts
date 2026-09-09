import { Component } from '@angular/core';
import { DocumentsComponent } from './features/documents/documents.component';
import { ChatComponent } from './features/chat/chat.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [DocumentsComponent, ChatComponent],
  template: `
    <div class="app-shell">
      <header class="app-header">
        <div class="brand">
          <span class="logo">◆</span>
          <div class="brand-text">
            <h1>Enterprise RAG Platform</h1>
            <p>On-prem document intelligence &amp; semantic search</p>
          </div>
        </div>
        <a class="api-link" href="http://localhost:8080/swagger" target="_blank" rel="noopener">
          API Docs ↗
        </a>
      </header>

      <main class="app-body">
        <aside class="pane pane--documents">
          <app-documents></app-documents>
        </aside>
        <section class="pane pane--chat">
          <app-chat></app-chat>
        </section>
      </main>
    </div>
  `,
  styles: [
    `
      .app-shell {
        display: flex;
        flex-direction: column;
        height: 100vh;
      }

      .app-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 0 24px;
        height: 64px;
        border-bottom: 1px solid var(--border);
        background: var(--surface);
      }

      .brand {
        display: flex;
        align-items: center;
        gap: 14px;
      }

      .logo {
        display: grid;
        place-items: center;
        width: 38px;
        height: 38px;
        border-radius: 10px;
        background: linear-gradient(135deg, var(--accent), var(--accent-soft));
        color: #fff;
        font-size: 18px;
      }

      .brand-text h1 {
        font-size: 16px;
        font-weight: 600;
        margin: 0;
      }

      .brand-text p {
        font-size: 12px;
        margin: 2px 0 0;
        color: var(--text-muted);
      }

      .api-link {
        font-size: 13px;
        font-weight: 500;
        color: var(--accent);
        text-decoration: none;
        padding: 8px 14px;
        border: 1px solid var(--border);
        border-radius: 8px;
        transition: background 0.15s ease;
      }

      .api-link:hover {
        background: var(--surface-hover);
      }

      .app-body {
        flex: 1;
        display: grid;
        grid-template-columns: 380px 1fr;
        min-height: 0;
      }

      .pane {
        min-height: 0;
        overflow: hidden;
      }

      .pane--documents {
        border-right: 1px solid var(--border);
        background: var(--surface);
      }

      .pane--chat {
        background: var(--bg);
      }

      @media (max-width: 900px) {
        .app-body {
          grid-template-columns: 1fr;
          grid-template-rows: minmax(220px, 40%) 1fr;
        }
        .pane--documents {
          border-right: none;
          border-bottom: 1px solid var(--border);
        }
      }
    `
  ]
})
export class AppComponent {}
