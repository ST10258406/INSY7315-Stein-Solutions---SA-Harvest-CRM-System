import { render, screen, waitFor, cleanup } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { useAgentStore } from '@/store/agentStore';
import { useAuthStore } from '@/store/authStore';
import { AgentDrawer } from './AgentDrawer';

vi.mock('@/lib/axios', () => ({ api: { post: vi.fn() } }));

function renderDrawer() {
  const queryClient = new QueryClient({ defaultOptions: { mutations: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <AgentDrawer open onOpenChange={() => {}} />
    </QueryClientProvider>,
  );
}

describe('AgentDrawer', () => {
  beforeEach(() => {
    vi.mocked(api.post).mockReset();
    useAgentStore.getState().reset();
  });

  afterEach(cleanup);

  it('sends the conversation and shows the reply with the tools used', async () => {
    const { conversationId } = useAgentStore.getState();
    vi.mocked(api.post).mockResolvedValue({
      data: {
        conversationId,
        reply: 'There are 4 pending donors.',
        toolCalls: [{ name: 'list_approvals' }, { name: 'search_donors' }, { name: 'search_donors' }],
        model: 'qwen2.5:7b',
      },
    });
    renderDrawer();

    await userEvent.type(screen.getByLabelText('Ask the CRM assistant'), 'How many pending?{Enter}');

    expect(await screen.findByText('There are 4 pending donors.')).toBeInTheDocument();
    expect(screen.getByText('Looked up: list_approvals, search_donors')).toBeInTheDocument();
    expect(api.post).toHaveBeenCalledWith('/api/v1/agent/messages', {
      conversationId,
      messages: [{ role: 'user', content: 'How many pending?' }],
    });
  });

  it('sends earlier turns with a follow-up question', async () => {
    const { conversationId, addMessage } = useAgentStore.getState();
    addMessage({ role: 'user', content: 'How many pending?' });
    addMessage({ role: 'assistant', content: 'Four.', toolCalls: [{ name: 'list_approvals' }] });
    vi.mocked(api.post).mockResolvedValue({ data: { conversationId, reply: 'Two.', toolCalls: [], model: null } });
    renderDrawer();

    await userEvent.type(screen.getByLabelText('Ask the CRM assistant'), 'In the Western Cape?{Enter}');

    await waitFor(() =>
      expect(api.post).toHaveBeenCalledWith('/api/v1/agent/messages', {
        conversationId,
        messages: [
          { role: 'user', content: 'How many pending?' },
          { role: 'assistant', content: 'Four.' },
          { role: 'user', content: 'In the Western Cape?' },
        ],
      }),
    );
  });

  it.each([
    [503, "The assistant isn't available right now."],
    [429, "You've hit the limit, try again in a few minutes."],
    [500, 'Something went wrong. Please try again.'],
  ])('shows a friendly message for a %i', async (status, message) => {
    vi.mocked(api.post).mockRejectedValue({
      isAxiosError: true,
      response: { status, data: { status, code: 'X', message: 'server detail at Foo.Bar()', errors: null, traceId: 't' } },
    });
    renderDrawer();

    await userEvent.type(screen.getByLabelText('Ask the CRM assistant'), 'Hi{Enter}');

    expect(await screen.findByRole('alert')).toHaveTextContent(message);
    expect(screen.queryByText(/server detail/)).not.toBeInTheDocument();
  });

  it('"New chat" clears the conversation', async () => {
    const before = useAgentStore.getState().conversationId;
    useAgentStore.getState().addMessage({ role: 'user', content: 'Old question' });
    renderDrawer();

    await userEvent.click(screen.getByRole('button', { name: 'New chat' }));

    expect(screen.queryByText('Old question')).not.toBeInTheDocument();
    expect(useAgentStore.getState().messages).toEqual([]);
    expect(useAgentStore.getState().conversationId).not.toBe(before);
  });

  it('is cleared on logout', () => {
    useAuthStore.setState({
      user: { id: '1', firstName: 'A', lastName: 'B', email: 'a@b.c', roles: ['SuperAdmin'] },
    } as Partial<ReturnType<typeof useAuthStore.getState>>);
    useAgentStore.getState().addMessage({ role: 'user', content: 'Question' });

    useAuthStore.getState().logout();

    expect(useAgentStore.getState().messages).toEqual([]);
  });
});
