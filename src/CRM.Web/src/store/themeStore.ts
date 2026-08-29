import { create } from 'zustand';
import { persist } from 'zustand/middleware';

export type Theme = 'light' | 'dark';

interface ThemeState {
  theme: Theme;
  setTheme: (theme: Theme) => void;
  toggleTheme: () => void;
}

function applyThemeClass(theme: Theme) {
  if (typeof document === 'undefined') return;
  document.documentElement.classList.toggle('dark', theme === 'dark');
}

// The app shipped dark-only up to this point, so dark stays the default for
// anyone without a stored preference — light mode is an opt-in addition.
// Apply the default immediately for non-persisted sessions.
applyThemeClass('dark');

export const useThemeStore = create<ThemeState>()(
  persist(
    (set, get) => ({
      // Default stays dark to match the app shell's historical behaviour.
      theme: 'dark',
      setTheme: (theme: Theme) => {
        applyThemeClass(theme);
        set({ theme });
      },
      toggleTheme: () => {
        const next = get().theme === 'dark' ? 'light' : 'dark';
        applyThemeClass(next);
        set({ theme: next });
      },
    }),
    {
      name: 'crm-theme',
      // Ensure the persisted theme is applied to the document when rehydration completes.
      onRehydrateStorage: () => (state) => {
        if (state?.theme) applyThemeClass(state.theme);
      },
    }
  )
);
