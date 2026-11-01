import type { ApiError } from '@/features/donors/types';

export type { ApiError };

// Mirrors SendAgentMessageCommand / AgentReplyDto (CRM.Application.Modules.Agent) — keep in sync.
export interface AgentMessage {
  role: 'user' | 'assistant';
  content: string;
  /** Assistant turns only — the tools the agent called to produce this reply. */
  toolCalls?: AgentToolCall[];
}

export interface AgentToolCall {
  name: string;
}

export interface SendAgentMessageRequest {
  conversationId: string;
  messages: { role: AgentMessage['role']; content: string }[];
}

export interface AgentReplyDto {
  conversationId: string;
  reply: string;
  toolCalls: AgentToolCall[];
  model: string | null;
}
