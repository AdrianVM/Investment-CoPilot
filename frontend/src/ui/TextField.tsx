import { type InputHTMLAttributes, type ReactNode, useId } from 'react'
import { classes } from './classes'

type Props = Omit<InputHTMLAttributes<HTMLInputElement>, 'id'> & {
  label: string
  /** Short help shown under the input and linked to it for screen readers. */
  hint?: string
  /** Shown beside the input (e.g. a submit button); wraps below it on narrow screens. */
  trailing?: ReactNode
}

export function TextField({ label, hint, trailing, className, ...inputProps }: Props) {
  const id = useId()
  const hintId = hint ? `${id}-hint` : undefined
  return (
    <div className={classes('text-field', className)}>
      <label className="text-field__label" htmlFor={id}>
        {label}
      </label>
      <div className="text-field__control">
        <input id={id} className="text-field__input" aria-describedby={hintId} {...inputProps} />
        {trailing}
      </div>
      {hint && (
        <p id={hintId} className="text-field__hint">
          {hint}
        </p>
      )}
    </div>
  )
}
