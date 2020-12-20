// https://github.com/vercel/next.js/discussions/11209
export function sanitizeProps<T extends Object>(obj: T): T {
  if (typeof obj === "object") {
    for (const key in obj) {
      const value = obj[key];

      if (typeof value === "undefined") {
        delete obj[key];
      } else {
        obj[key] = sanitizeProps(value);
      }
    }
  }

  return obj;
}
