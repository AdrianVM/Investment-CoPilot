import type { ReactNode } from 'react'

type Tone = 'info' | 'warning' | 'danger'

/** The text lead names the kind of notice, so it never depends on colour alone. */
const leads: Record<Tone, string> = {
  info: 'Note:',
  warning: 'Out of date:',
  danger: 'Error:',
}

type Props = {
  tone: Tone
  /** Overrides the default lead, e.g. "Not found:". */
  lead?: string
  /**
   * Announce to screen readers as soon as it appears. Defaults to true for errors; set it for other notices
   * that answer something the user just did, such as a lookup that found nothing.
   */
  announce?: boolean
  children: ReactNode
}

/** A short message about the state of the page. */
export function Notice({ tone, lead = leads[tone], announce = tone === 'danger', children }: Props) {
  return (
    <p role={announce ? 'alert' : undefined} className={`notice notice--${tone}`}>
      <NoticeIcon tone={tone} />
      <strong className="notice__lead">{lead}</strong> {children}
    </p>
  )
}

function NoticeIcon({ tone }: { tone: Tone }) {
  return (
    <svg className="notice__icon" viewBox="0 0 16 16" width="16" height="16" aria-hidden="true" focusable="false">
      {tone === 'info' ? (
        <path fill="currentColor" d="M8 1a7 7 0 1 0 0 14A7 7 0 0 0 8 1Zm-.9 3.5h1.8v1.8H7.1V4.5Zm0 3h1.8v4H7.1v-4Z" />
      ) : tone === 'warning' ? (
        <path fill="currentColor" d="M8 1.2 15.2 14H.8L8 1.2Zm-.9 4.3v4h1.8v-4H7.1Zm0 5.2v1.8h1.8v-1.8H7.1Z" />
      ) : (
        <path
          fill="currentColor"
          d="M5.1 1h5.8L15 5.1v5.8L10.9 15H5.1L1 10.9V5.1L5.1 1Zm2 3.5v4.5h1.8V4.5H7.1Zm0 5.7V12h1.8v-1.8H7.1Z"
        />
      )}
    </svg>
  )
}
