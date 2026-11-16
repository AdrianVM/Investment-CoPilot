/// <reference types="vitest/config" />
import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv, type Plugin } from 'vite'
import { contentSecurityPolicy } from './csp.ts'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), 'VITE_')

  const cspPlugin: Plugin = {
    name: 'content-security-policy',
    apply: 'build',
    transformIndexHtml: () => [
      {
        tag: 'meta',
        attrs: { 'http-equiv': 'Content-Security-Policy', content: contentSecurityPolicy(env) },
        injectTo: 'head-prepend',
      },
    ],
  }

  return {
    plugins: [react(), cspPlugin],
    server: {
      port: 5173,
      strictPort: true, // the sign-in provider only allows redirects back to this port
      proxy: {
        // The API (dotnet run in backend/src/Host) listens on 5080; same-origin requests avoid CORS.
        '/api': 'http://localhost:5080',
      },
    },
    preview: {
      port: 5173,
      strictPort: true,
      proxy: {
        '/api': 'http://localhost:5080',
      },
    },
    test: {
      environment: 'jsdom',
      setupFiles: ['./src/test/setup.ts'],
    },
  }
})
