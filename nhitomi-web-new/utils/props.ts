export function sanitizeProps<T extends Object>(obj: T): T {
  deleteUndefinedRecursive(obj);
  return obj;
}

// https://github.com/vercel/next.js/discussions/11209
function deleteUndefinedRecursive(obj: any) {
  if (typeof obj !== "object") return;

  for (const key in obj) {
    const value = obj[key];

    if (typeof value === "undefined") {
      delete obj[key];
    } else {
      sanitizeProps(value);
    }
  }
}
