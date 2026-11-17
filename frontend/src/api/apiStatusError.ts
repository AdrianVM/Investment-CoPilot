/**
 * A failed API request, with its HTTP status so messages (and the query client's retry policy, which skips 4xx)
 * can depend on it. Thrown by query functions that call the generated client with throwOnError off.
 */
export class ApiStatusError extends Error {
  readonly status: number

  constructor(status: number, what: string) {
    super(`${what} failed with HTTP ${status}`)
    this.status = status
  }
}
