import { useMutation } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import type { AgentReplyDto, ApiError, SendAgentMessageRequest } from '../types';

/**
 * POST /api/v1/agent/messages — one stateless agent turn (the whole conversation so far
 * is sent every time). SuperAdmin only, enforced by the API.
 * No invalidateQueries on success: the agent's tools are read-only and change no server state.
 */
export function useSendAgentMessage() {
  return useMutation<AgentReplyDto, ApiError, SendAgentMessageRequest>({
    mutationFn: async (request) => {
      const { data } = await api.post<AgentReplyDto>('/api/v1/agent/messages', request);
      return data;
    },
  });
}
