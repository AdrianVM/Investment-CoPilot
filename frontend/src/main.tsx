import { QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './styles/tokens.css'
import './styles/base.css'
import './ui/ui.css'
import App from './App.tsx'
import { configureApiClient, createQueryClient } from './api/apiClient.ts'
import { AuthProvider } from './auth/AuthProvider.tsx'
import { silentCallbackPath } from './auth/oidcSettings.ts'
import { userManager } from './auth/userManager.ts'

if (window.location.pathname === silentCallbackPath) {
  // Loaded in the hidden iframe used for silent token renewal: hand the result to the parent window.
  void userManager.signinSilentCallback()
} else {
  configureApiClient(import.meta.env.VITE_API_BASE_URL ?? '', async () => (await userManager.getUser())?.access_token)

  const queryClient = createQueryClient()
  // Never show one user's cached data to the next: drop everything when the user signs out.
  userManager.events.addUserUnloaded(() => queryClient.clear())

  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <QueryClientProvider client={queryClient}>
        <AuthProvider userManager={userManager}>
          <App />
        </AuthProvider>
      </QueryClientProvider>
    </StrictMode>,
  )
}
