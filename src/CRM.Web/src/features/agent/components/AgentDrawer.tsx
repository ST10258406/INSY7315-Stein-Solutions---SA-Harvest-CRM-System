import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import { useAgentStore } from '@/store/agentStore';
import { useSendAgentMessage } from '../hooks/useSendAgentMessage';
import { AgentMessageList } from './AgentMessageList';
import { AgentComposer } from './AgentComposer';
import type { ApiError } from '../types';

const SUGGESTED_PROMPTS = [
  'How many donors are pending approval?',
  'Which donors have a follow-up due this week?',
  'Give me a summary of the dashboard stats.',
];

// Friendly copy only — never the server message or a stack trace.
function errorMessage(error: ApiError): string {
  switch (error.response?.status) {
    case 503:
      return "The assistant isn't available right now.";
    case 429:
      return "You've hit the limit, try again in a few minutes.";
    default:
      return 'Something went wrong. Please try again.';
  }
}

interface AgentDrawerProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function AgentDrawer({ open, onOpenChange }: AgentDrawerProps) {
  const { conversationId, messages, addMessage, reset } = useAgentStore();
  const sendMessage = useSendAgentMessage();

  const send = (content: string) => {
    const history = [...messages, { role: 'user' as const, content }];
    addMessage({ role: 'user', content });
    sendMessage.mutate(
      { conversationId, messages: history.map(({ role, content }) => ({ role, content })) },
      {
        onSuccess: (reply) => {
          // Ignore a reply that lands after "New chat" started a different conversation.
          if (reply.conversationId !== useAgentStore.getState().conversationId) return;
          addMessage({ role: 'assistant', content: reply.reply, toolCalls: reply.toolCalls });
        },
      },
    );
  };

  const newChat = () => {
    reset();
    sendMessage.reset();
  };

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-[480px]">
        <SheetHeader>
          <SheetTitle>CRM assistant</SheetTitle>
          <SheetDescription>Read-only answers from live CRM data. Check anything important.</SheetDescription>
          <button
            type="button"
            onClick={newChat}
            className="mt-3 rounded-full border border-border px-3.5 py-1.5 text-xs font-bold text-foreground transition-colors hover:bg-hover"
          >
            New chat
          </button>
        </SheetHeader>

        <div className="min-h-0 flex-1 overflow-y-auto px-6.5 py-4">
          {messages.length === 0 && !sendMessage.isPending ? (
            <div className="flex flex-col gap-2">
              <p className="m-0 text-[12.5px] font-semibold text-muted-foreground">Try asking:</p>
              {SUGGESTED_PROMPTS.map((prompt) => (
                <button
                  key={prompt}
                  type="button"
                  onClick={() => send(prompt)}
                  className="rounded-2xl border border-border bg-field px-3.75 py-2.5 text-left text-[13px] font-medium text-foreground transition-colors hover:bg-hover"
                >
                  {prompt}
                </button>
              ))}
            </div>
          ) : (
            <AgentMessageList messages={messages} isPending={sendMessage.isPending} />
          )}
          {sendMessage.error && (
            <p role="alert" className="mt-3 text-[12.5px] font-semibold text-destructive">
              {errorMessage(sendMessage.error)}
            </p>
          )}
        </div>

        <div className="flex-shrink-0 border-t border-[var(--divider)] px-6.5 py-4">
          <AgentComposer disabled={sendMessage.isPending} onSend={send} />
        </div>
      </SheetContent>
    </Sheet>
  );
}
