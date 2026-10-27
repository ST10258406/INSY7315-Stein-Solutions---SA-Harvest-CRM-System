import { z } from 'zod';

/** Mirrors Agent:MaxMessageLength on the API (SendAgentMessageCommandValidator). */
export const AGENT_MESSAGE_MAX_LENGTH = 2000;

/** Mirror Agent:MaxHistoryMessages and Agent:MaxAssistantMessageLength — the request history is trimmed to fit. */
export const AGENT_MAX_HISTORY_MESSAGES = 20;
export const AGENT_ASSISTANT_MAX_LENGTH = 8000;

export const agentMessageSchema = z
  .string()
  .trim()
  .min(1, 'Type a question first.')
  .max(AGENT_MESSAGE_MAX_LENGTH, `Keep it under ${AGENT_MESSAGE_MAX_LENGTH} characters.`);
