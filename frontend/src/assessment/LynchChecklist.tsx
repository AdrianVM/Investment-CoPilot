import { useQuery } from '@tanstack/react-query'
import { useId } from 'react'
import { ApiStatusError } from '../api/apiStatusError'
import { getStockAssessmentQueryKey } from '../api/generated/@tanstack/react-query.gen'
import { getStockAssessment } from '../api/generated/sdk.gen'
import type { CriterionAssessment, Measure, StockAssessment, Verdict } from '../api/generated/types.gen'
import {
  formatCompactMoney,
  formatDateTime,
  formatPercent,
  formatRate,
  formatRatio,
  formatThreshold,
} from '../market-data/format'
import { Notice } from '../ui/Notice'

const strategy = 'peter-lynch'

async function fetchAssessment(symbol: string, signal: AbortSignal) {
  const { data, response } = await getStockAssessment({ path: { strategy, symbol }, signal })
  if (!data) {
    throw new ApiStatusError(response?.status ?? 0, 'Assessment')
  }
  return data
}

/**
 * The stock checked against Peter Lynch's six criteria (GET /api/assessments/peter-lynch/stocks/{symbol}).
 * Loaded separately from the figures, so a failure here never hides them.
 */
export function LynchChecklist({ symbol }: { symbol: string }) {
  const query = useQuery({
    queryKey: getStockAssessmentQueryKey({ path: { strategy, symbol } }),
    queryFn: ({ signal }) => fetchAssessment(symbol, signal),
  })

  if (query.isPending) {
    return <p className="checklist__status">Checking the Peter Lynch criteria…</p>
  }
  if (query.isError) {
    return (
      <Notice tone="danger">The Peter Lynch checklist could not be loaded. The figures above are not affected.</Notice>
    )
  }
  return <Checklist assessment={query.data} />
}

function Checklist({ assessment }: { assessment: StockAssessment }) {
  const headingId = useId()
  const { met, cannotAssess, total } = assessment.summary
  return (
    <section className="checklist" aria-labelledby={headingId}>
      <header className="checklist__header">
        <h3 id={headingId}>{assessment.strategyName} checklist</h3>
        <p className="checklist__summary">
          Meets {met} of {total} criteria.
          {cannotAssess > 0 && ` ${cannotAssess} could not be assessed.`}
        </p>
      </header>
      <ul className="checklist__items">
        {assessment.criteria.map((criterion) => (
          <CriterionItem key={criterion.id} criterion={criterion} />
        ))}
      </ul>
      <p className="checklist__disclaimer">
        For information only, not investment advice. Based on {assessment.source}, as of{' '}
        {formatDateTime(assessment.asOf)}.
      </p>
    </section>
  )
}

const verdictText: Record<Verdict, string> = {
  meets: 'Meets',
  doesNotMeet: 'Does not meet',
  cannotAssess: 'Cannot be assessed',
}

function CriterionItem({ criterion }: { criterion: CriterionAssessment }) {
  const single = criterion.measures.length === 1
  return (
    <li className={`checklist__item checklist__item--${criterion.verdict}`}>
      <span className="checklist__verdict">
        <VerdictIcon verdict={criterion.verdict} />
        {verdictText[criterion.verdict]}
        <span className="visually-hidden">:</span>
      </span>{' '}
      <span className="checklist__name">{criterion.name}</span>{' '}
      <span className="checklist__detail">
        {showReasonOnly(criterion) ? (
          reasonText(criterion)
        ) : criterion.conversion ? (
          <ConversionDetail criterion={criterion} />
        ) : (
          criterion.measures.map((measure, i) => (
            <span key={measure.id}>
              {i > 0 && ', '}
              <MeasureText measure={measure} withLabel={!single} />
            </span>
          ))
        )}
      </span>
    </li>
  )
}

/** Show only the reason when no value is known; otherwise list the values, so a partly known criterion shows what is missing. */
function showReasonOnly(criterion: CriterionAssessment) {
  return (
    criterion.reason === 'exchangeRateNotAvailable' ||
    (criterion.reason !== null && criterion.measures.every((m) => m.value === null))
  )
}

function reasonText(criterion: CriterionAssessment) {
  return criterion.reason === 'exchangeRateNotAvailable'
    ? 'No exchange rate available to convert to US dollars'
    : 'Data not available'
}

function MeasureText({ measure, withLabel }: { measure: Measure; withLabel: boolean }) {
  const comparison = measure.comparison === 'lessThan' ? 'below' : 'above'
  return (
    <>
      {withLabel && `${measure.label} `}
      {measure.value === null ? 'not available' : <span className="figure">{formatValue(measure, measure.value)}</span>}{' '}
      ({comparison} {formatThresholdOf(measure)})
    </>
  )
}

function ConversionDetail({ criterion }: { criterion: CriterionAssessment }) {
  const conversion = criterion.conversion!
  const measure = criterion.measures[0]
  return (
    <>
      <span className="figure">{formatCompactMoney(conversion.original)}</span>, about{' '}
      <span className="figure">{formatCompactMoney(conversion.converted)}</span> (
      {measure.comparison === 'lessThan' ? 'below' : 'above'} {formatThresholdOf(measure)}). Converted at 1{' '}
      {conversion.original.currency} = {formatRate(conversion.rate)} {conversion.converted.currency} ({conversion.source}
      , as of {formatDateTime(conversion.asOf)}).
    </>
  )
}

function formatValue(measure: Measure, value: number) {
  switch (measure.unit) {
    case 'fraction':
      return formatPercent(value)
    case 'money':
      return formatCompactMoney({ amount: value, currency: measure.currency ?? 'USD' })
    default:
      return formatRatio(value)
  }
}

function formatThresholdOf(measure: Measure) {
  switch (measure.unit) {
    case 'fraction':
      return formatPercent(measure.threshold)
    case 'money':
      return formatCompactMoney({ amount: measure.threshold, currency: measure.currency ?? 'USD' })
    default:
      return formatThreshold(measure.threshold)
  }
}

function VerdictIcon({ verdict }: { verdict: Verdict }) {
  return (
    <svg className="checklist__icon" viewBox="0 0 16 16" width="16" height="16" aria-hidden="true" focusable="false">
      {verdict === 'meets' ? (
        <path fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" d="M3 8.5 6.5 12 13 4.5" />
      ) : verdict === 'doesNotMeet' ? (
        <path fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" d="M4 4l8 8M12 4l-8 8" />
      ) : (
        <path fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" d="M3.5 8h9" />
      )}
    </svg>
  )
}
