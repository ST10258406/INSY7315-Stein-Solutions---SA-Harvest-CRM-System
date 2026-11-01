import { create } from 'zustand';
import { useAuthStore } from './authStore';
import type { AgentMessage } from '@/features/agent/types';

// The open agent conversation. In memory only — no localStorage/sessionStorage, ever.

interface AgentState {
  conversationId: string;
  messages: AgentMessage[];
  addMessage: (message: AgentMessage) => void;
  /** "New chat" — drops the conversation and starts a fresh id. */
  reset: () => void;
}

export const useAgentStore = create<AgentState>((set) => ({
  conversationId: crypto.randomUUID(),
  messages: [],
  addMessage: (message) => set((state) => ({ messages: [...state.messages, message] })),
  reset: () => set({ conversationId: crypto.randomUUID(), messages: [] }),
}));

// Clear the conversation whenever the session ends, whichever logout path triggered it.
useAuthStore.subscribe((state, prev) => {
  if (prev.user && !state.user) useAgentStore.getState().reset();
});
