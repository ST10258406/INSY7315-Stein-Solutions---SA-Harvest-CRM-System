import { renderHook, waitFor, cleanup } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { api } from '@/lib/axios';
import { useSendAgentMessage } from './useSendAgentMessage';
import type { AgentReplyDto, SendAgentMessageRequest } from '../types';

vi.mock('@/lib/axios', () => ({ api: { post: vi.fn() } }));

function wrapper({ children }: { children: ReactNode }) {
  const queryClient = new QueryClient({ defaultOptions: { mutations: { retry: false } } });
  return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
}

describe('useSendAgentMessage', () => {
  beforeEach(() => vi.clearAllMocks());
  afterEach(cleanup);

  it('POSTs the conversation through the shared axios instance and returns the reply', async () => {
    const request: SendAgentMessageRequest = {
      conversationId: 'c1',
      messages: [{ role: 'user', content: 'How many donors?' }],
    };
    const reply: AgentReplyDto = { conversationId: 'c1', reply: 'Forty.', toolCalls: [], model: 'm' };
    vi.mocked(api.post).mockResolvedValue({ data: reply });

    const { result } = renderHook(() => useSendAgentMessage(), { wrapper });
    result.current.mutate(request);

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.post).toHaveBeenCalledWith('/api/v1/agent/messages', request);
    expect(result.current.data).toEqual(reply);
  });
});
