import { describe, it, expect, beforeEach } from 'vitest';
import { useAgentStore } from './agentStore';
import { useAuthStore } from './authStore';

describe('agentStore', () => {
  beforeEach(() => useAgentStore.getState().reset());

  it('is cleared with a new conversation id on logout', () => {
    useAuthStore.setState({
      user: { id: '1', firstName: 'A', lastName: 'B', email: 'a@b.c', roles: ['SuperAdmin'] },
    } as Partial<ReturnType<typeof useAuthStore.getState>>);
    const before = useAgentStore.getState().conversationId;
    useAgentStore.getState().addMessage({ role: 'user', content: 'Question' });

    useAuthStore.getState().logout();

    expect(useAgentStore.getState().messages).toEqual([]);
    expect(useAgentStore.getState().conversationId).not.toBe(before);
  });

  it('keeps the conversation while the user stays signed in', () => {
    useAgentStore.getState().addMessage({ role: 'user', content: 'Question' });

    useAuthStore.setState({ accessToken: 'refreshed' } as Partial<ReturnType<typeof useAuthStore.getState>>);

    expect(useAgentStore.getState().messages).toHaveLength(1);
  });

  it('never writes the conversation to web storage', () => {
    useAgentStore.getState().addMessage({ role: 'user', content: 'Secret donor question' });

    expect(JSON.stringify({ ...localStorage })).not.toContain('Secret donor question');
    expect(JSON.stringify({ ...sessionStorage })).not.toContain('Secret donor question');
  });
});
