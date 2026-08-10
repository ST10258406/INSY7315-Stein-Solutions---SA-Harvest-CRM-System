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
      // edits made on the host. Set VITE_USE_POLLING=true in that setup only —
      // polling raises CPU usage and isn't needed on native filesystems.
      usePolling: process.env.VITE_USE_POLLING === 'true',
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