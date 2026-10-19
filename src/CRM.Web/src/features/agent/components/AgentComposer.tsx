import { useState, type FormEvent, type KeyboardEvent } from 'react';
import { SendHorizontal } from 'lucide-react';
import { agentMessageSchema, AGENT_MESSAGE_MAX_LENGTH } from '../schemas/agentMessageSchema';

interface AgentComposerProps {
  disabled: boolean;
  onSend: (content: string) => void;
}

/** Enter sends, Shift+Enter inserts a newline. */
export function AgentComposer({ disabled, onSend }: AgentComposerProps) {
  const [value, setValue] = useState('');
  const [error, setError] = useState<string | null>(null);

  const submit = () => {
    const parsed = agentMessageSchema.safeParse(value);
    if (!parsed.success) {
      setError(parsed.error.issues[0].message);
      return;
    }
    setError(null);
    setValue('');
    onSend(parsed.data);
  };

  const onSubmit = (event: FormEvent) => {
    event.preventDefault();
    submit();
  };

  const onKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      if (!disabled) submit();
    }
  };

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-1.5">
      <div className="flex items-end gap-2">
        <textarea
          aria-label="Ask the CRM assistant"
          rows={2}
          value={value}
          maxLength={AGENT_MESSAGE_MAX_LENGTH}
          disabled={disabled}
          onChange={(event) => setValue(event.target.value)}
          onKeyDown={onKeyDown}
          placeholder="Ask about donors, tasks, approvals…"
          className="min-w-0 flex-1 resize-none rounded-2xl border border-border bg-field px-3.75 py-3 text-[13.5px] font-medium leading-relaxed text-foreground outline-none placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-brand/60 disabled:opacity-60"
        />
        <button
          type="submit"
          aria-label="Send"
          disabled={disabled}
          className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-brand text-white transition-opacity disabled:opacity-50"
        >
          <SendHorizontal className="h-4 w-4" />
        </button>
      </div>
      {error && <p className="m-0 text-[11.5px] font-semibold text-destructive">{error}</p>}
    </form>
  );
}
