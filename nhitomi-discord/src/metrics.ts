/** Generates a geometric sequence for use as histogram bins. */
export function getBuckets(min: number, max: number, count: number, round = 6) {
  round = Math.pow(10, round);

  const r = Math.pow(max / min, 1 / (count - 1));
  const a: number[] = [];

  for (let i = 0; i < count; i++)
    a[i] = Math.round(min * Math.pow(r, i) * round) / round;

  return a;
}
