import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { FigureSheet } from './FigureSheet'

describe('FigureSheet', () => {
  it('pairs each figure with its label and styles figures as tabular numerals', () => {
    render(
      <FigureSheet
        title="Lynchpin Tools Inc."
        aside="LYNCH"
        rows={[
          { label: 'Price', value: '$42.50' },
          { label: 'Forward P/E', value: 'Not available', missing: true },
        ]}
        footer="Sample data."
      />,
    )

    expect(screen.getByRole('article', { name: 'Lynchpin Tools Inc.' })).toBeInTheDocument()
    const price = screen.getByText('Price', { selector: 'dt' }).nextElementSibling
    expect(price?.tagName).toBe('DD')
    expect(price).toHaveTextContent('$42.50')
    expect(price).toHaveClass('figure')

    const missing = screen.getByText('Forward P/E', { selector: 'dt' }).nextElementSibling
    expect(missing).toHaveTextContent('Not available')
    expect(missing).not.toHaveClass('figure')

    expect(screen.getByText('LYNCH')).toBeInTheDocument()
    expect(screen.getByText('Sample data.')).toBeInTheDocument()
  })
})
