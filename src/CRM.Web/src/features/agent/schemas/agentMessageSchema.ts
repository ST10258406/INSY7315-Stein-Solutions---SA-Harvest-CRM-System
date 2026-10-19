import { z } from 'zod';

/** Mirrors Agent:MaxMessageLength on the API (SendAgentMessageCommandValidator). */
export const AGENT_MESSAGE_MAX_LENGTH = 2000;

export const agentMessageSchema = z
  .string()
  .trim()
  .min(1, 'Type a question first.')
  .max(AGENT_MESSAGE_MAX_LENGTH, `Keep it under ${AGENT_MESSAGE_MAX_LENGTH} characters.`);
