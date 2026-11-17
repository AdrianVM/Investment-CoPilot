import type { CriterionAssessment, StockAssessment } from '../api/generated/types.gen'

// Assessment responses for tests, shaped like the API's (see backend AssessmentServiceTests for the real results).

const asOf = '2026-11-17T09:30:00Z'

function criterion(partial: Partial<CriterionAssessment> & Pick<CriterionAssessment, 'id' | 'name'>): CriterionAssessment {
  return { verdict: 'meets', measures: [], reason: null, conversion: null, ...partial }
}

export const lynchAssessment: StockAssessment = {
  strategyId: 'peter-lynch',
  strategyName: 'Peter Lynch',
  symbol: 'LYNCH',
  companyName: 'Lynchpin Tools Inc. (sample)',
  source: 'Sample data (fake provider)',
  asOf,
  isStale: false,
  criteria: [
    criterion({
      id: 'pe',
      name: 'P/E',
      measures: [
        { id: 'trailingPe', label: 'Trailing P/E', value: 14.2, unit: 'ratio', currency: null, comparison: 'lessThan', threshold: 25 },
        { id: 'forwardPe', label: 'Forward P/E', value: 11.8, unit: 'ratio', currency: null, comparison: 'lessThan', threshold: 15 },
      ],
    }),
    criterion({
      id: 'institutionalOwnership',
      name: 'Institutional ownership',
      measures: [{ id: 'institutionalOwnership', label: 'Institutional ownership', value: 0.07, unit: 'fraction', currency: null, comparison: 'lessThan', threshold: 0.1 }],
    }),
    criterion({
      id: 'insiderOwnership',
      name: 'Insider ownership',
      measures: [{ id: 'insiderOwnership', label: 'Insider ownership', value: 0.26, unit: 'fraction', currency: null, comparison: 'greaterThan', threshold: 0.2 }],
    }),
    criterion({
      id: 'epsGrowth',
      name: 'EPS growth',
      measures: [{ id: 'epsGrowth', label: 'EPS growth (year over year)', value: 0.21, unit: 'fraction', currency: null, comparison: 'greaterThan', threshold: 0.15 }],
    }),
    criterion({
      id: 'debtToEquity',
      name: 'Debt to equity',
      measures: [{ id: 'debtToEquity', label: 'Debt to equity', value: 0.18, unit: 'ratio', currency: null, comparison: 'lessThan', threshold: 0.35 }],
    }),
    criterion({
      id: 'marketCap',
      name: 'Market capitalisation',
      measures: [{ id: 'marketCap', label: 'Market capitalisation', value: 1_800_000_000, unit: 'money', currency: 'USD', comparison: 'lessThan', threshold: 5_000_000_000 }],
    }),
  ],
  summary: { met: 6, notMet: 0, cannotAssess: 0, total: 6 },
}
