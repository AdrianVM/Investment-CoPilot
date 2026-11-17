import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { Button } from './Button'
import { Notice } from './Notice'
import { TextField } from './TextField'

describe('Button', () => {
  it.each(['primary', 'secondary'] as const)('%s renders a native button that is not a submit by default', async (variant) => {
    const onClick = vi.fn<() => void>()
    render(
      <Button variant={variant} onClick={onClick}>
        Look up
      </Button>,
    )

    const button = screen.getByRole('button', { name: 'Look up' })
    expect(button.tagName).toBe('BUTTON')
    expect(button).toHaveAttribute('type', 'button')
    expect(button).toHaveClass('button', `button--${variant}`)
    await userEvent.click(button)
    expect(onClick).toHaveBeenCalledOnce()
  })

  it('can be a submit button', () => {
    render(<Button type="submit">Go</Button>)

    expect(screen.getByRole('button', { name: 'Go' })).toHaveAttribute('type', 'submit')
  })
})

describe('TextField', () => {
  it('associates its label and hint with the input', () => {
    render(<TextField label="Ticker symbol" hint="For example LYNCH" />)

    const input = screen.getByLabelText('Ticker symbol')
    expect(input.tagName).toBe('INPUT')
    expect(input).toHaveAccessibleDescription('For example LYNCH')
  })

  it('passes input props through', async () => {
    const onChange = vi.fn<() => void>()
    render(<TextField label="Ticker symbol" value="" onChange={onChange} />)

    await userEvent.type(screen.getByLabelText('Ticker symbol'), 'A')

    expect(onChange).toHaveBeenCalled()
  })
})

describe('Notice', () => {
  it.each([
    ['info', 'Note:'],
    ['warning', 'Out of date:'],
    ['danger', 'Error:'],
  ] as const)('%s notice leads with "%s" so it does not rely on colour', (tone, lead) => {
    const { container } = render(<Notice tone={tone}>Something happened.</Notice>)

    expect(container).toHaveTextContent(`${lead} Something happened.`)
    expect(container.querySelector('svg')).toHaveAttribute('aria-hidden', 'true')
  })

  it('announces errors only', () => {
    render(
      <>
        <Notice tone="danger">Broken.</Notice>
        <Notice tone="warning">Old.</Notice>
      </>,
    )

    expect(screen.getByRole('alert')).toHaveTextContent('Error: Broken.')
  })

  it('can announce a non-error notice', () => {
    render(
      <Notice tone="info" announce>
        Nothing found.
      </Notice>,
    )

    expect(screen.getByRole('alert')).toHaveTextContent('Note: Nothing found.')
  })

  it('accepts a custom lead', () => {
    const { container } = render(
      <Notice tone="info" lead="Not found:">
        No stock for XYZ.
      </Notice>,
    )

    expect(container).toHaveTextContent('Not found: No stock for XYZ.')
  })
})
