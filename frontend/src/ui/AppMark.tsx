/** The app mark: a ruled sheet with a pen check across its edge. Follows the text colour via currentColor. */
export function AppMark({ size = 28 }: { size?: number }) {
  return (
    <svg
      className="app-mark"
      viewBox="0 0 24 24"
      width={size}
      height={size}
      fill="none"
      stroke="currentColor"
      strokeWidth="1.6"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      focusable="false"
    >
      <rect x="3.5" y="2.5" width="14" height="19" rx="1.5" />
      <path d="M7 7.5h7M7 11h4.5M7 14.5h3" />
      <path d="M12.5 16.5l2.5 2.5 6.5-8.5" strokeWidth="2" />
    </svg>
  )
}
