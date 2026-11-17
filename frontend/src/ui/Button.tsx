import type { ButtonHTMLAttributes } from 'react'
import { classes } from './classes'

type Props = ButtonHTMLAttributes<HTMLButtonElement> & {
  /** primary: the one main action in a region (ink fill). secondary: everything else (outlined). */
  variant?: 'primary' | 'secondary'
}

export function Button({ variant = 'secondary', type = 'button', className, ...props }: Props) {
  return <button type={type} className={classes('button', `button--${variant}`, className)} {...props} />
}
