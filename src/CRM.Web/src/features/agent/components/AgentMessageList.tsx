import type { AgentMessage } from '../types';
import { AgentMarkdown } from './AgentMarkdown';

interface AgentMessageListProps {
  messages: AgentMessage[];
  isPending: boolean;
}

/**
 * User messages render as plain text. Assistant replies render through AgentMarkdown, which
 * builds React elements from a small Markdown subset — never raw HTML, never
 * dangerouslySetInnerHTML — so model output can't become an XSS vector.
 */
export function AgentMessageList({ messages, isPending }: AgentMessageListProps) {
  return (
    <div aria-live="polite" className="flex flex-col gap-3">
      {messages.map((message, index) => {
        const toolNames = [...new Set(message.toolCalls?.map((call) => call.name) ?? [])];
        const isUser = message.role === 'user';
        return (
          <div key={index} className={`flex flex-col gap-1.5 ${isUser ? 'items-end' : 'items-start'}`}>
            <div
              className={`max-w-[85%] rounded-2xl has-[table]:w-full has-[table]:max-w-full px-3.75 py-2.5 text-[13.5px] font-medium leading-relaxed ${
                isUser ? 'whitespace-pre-wrap bg-brand text-white' : 'border border-border bg-field text-foreground'
              }`}
            >
              {isUser ? message.content : <AgentMarkdown content={message.content} />}
            </div>
            {toolNames.length > 0 && (
              <p className="m-0 text-[11.5px] font-semibold text-muted-foreground">Looked up: {toolNames.join(', ')}</p>
            )}
          </div>
        );
      })}
      {isPending && (
        <div className="self-start rounded-2xl border border-border bg-field px-3.75 py-2.5 text-[13.5px] font-medium text-muted-foreground">
          Thinking…
        </div>
      )}
    </div>
  );
}
