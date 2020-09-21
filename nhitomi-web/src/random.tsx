export function randomStr(length: number) {
  return [...Array(length)].map(() => Math.random().toString(36)[2]).join("");
}