import { type ReactNode, useId } from 'react'

export type FigureRow = {
  label: string
  value: string
  /** Set when the value is a placeholder such as "Not available", so it is not styled as a figure. */
  missing?: boolean
}

type Props = {
  title: ReactNode
  /** Shown opposite the title, e.g. the ticker symbol. */
  aside?: ReactNode
  rows: FigureRow[]
  footer?: ReactNode
}

/**
 * A set of labelled figures, e.g. a stock's metrics. Labels left, figures right in tabular numerals;
 * one column on narrow screens, two from 40rem. Rendered as a description list so each figure stays
 * associated with its label for screen readers.
 */
export function FigureSheet({ title, aside, rows, footer }: Props) {
  const headingId = useId()
  return (
    <article className="figure-sheet" aria-labelledby={headingId}>
      <header className="figure-sheet__header">
        <h3 id={headingId} className="figure-sheet__title">
          {title}
        </h3>
        {aside && <span className="figure-sheet__aside">{aside}</span>}
      </header>
      <dl className="figure-sheet__rows">
        {rows.map((row) => (
          <div key={row.label} className="figure-sheet__row">
            <dt className="figure-sheet__label">{row.label}</dt>
            <dd className={row.missing ? 'figure-sheet__value figure-sheet__value--missing' : 'figure-sheet__value figure'}>
              {row.value}
            </dd>
          </div>
        ))}
      </dl>
      {footer && <footer className="figure-sheet__footer">{footer}</footer>}
    </article>
  )
}
