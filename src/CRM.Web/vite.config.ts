/// <reference types="vitest/config" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
  ],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 3000, // Ensure it runs on port 3000
    host: true,
    watch: {
      // Docker Desktop on Windows doesn't reliably forward native fs change
      // events through the bind mount, so Vite's default watcher misses
      // edits made on the host. Polling works regardless of the backend.
      usePolling: true,
    },
    proxy: {
      '/api': {
        target: 'http://localhost:5278',
        changeOrigin: true,
        secure: false,
      },
    },
  },
  test: {
    environment: 'jsdom',
  },
})