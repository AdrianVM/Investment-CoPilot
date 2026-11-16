import { QueryClient } from '@tanstack/react-query'
import { client } from './generated/client.gen'

/**
 * Points the generated API client at the API and makes it send the current access token.
 * The token is read on every request, so silently renewed tokens are picked up automatically.
 */
export function configureApiClient(baseUrl: string, getAccessToken: () => Promise<string | undefined>) {
  client.setConfig({ baseUrl, auth: getAccessToken })
}

export function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        // Do not retry authentication or not-found failures; they will not fix themselves.
        retry: (failureCount, error) => failureCount < 2 && !isClientError(error),
      },
    },
  })
}

function isClientError(error: unknown) {
  const status = (error as { status?: number } | null)?.status
  return typeof status === 'number' && status >= 400 && status < 500
}
