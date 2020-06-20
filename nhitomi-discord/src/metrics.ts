import { Histogram, LabelValues } from 'prom-client'
import { performance } from 'perf_hooks'

/** Generates a geometric sequence for use as histogram bins. */
export function getBuckets(min: number, max: number, count: number, round = 2) {
  round = Math.pow(10, round)

  const r = Math.pow(max / min, 1 / (count - 1))
  const a: number[] = []

  for (let i = 0; i < count; i++)
    a[i] = Math.round(min * Math.pow(r, i) * round) / round

  return a
}

/** Accurately measures time and observes the histogram with the given unit. */
export function measureHistogram<T extends string>(histogram: Histogram<T>, labels?: LabelValues<T>, unit: 'ms' | 's' = 'ms') {
  const start = performance.now()

  return () => {
    let elapsed = performance.now() - start

    switch (unit) {
      case 's':
        elapsed /= 1000
        break
    }

    if (labels)
      histogram.observe(labels, elapsed)
    else
      histogram.observe(elapsed)
  }
}
