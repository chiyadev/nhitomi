export function sanitizeProps<T extends Object>(obj: T): T {
  deleteUndefinedRecursive(obj);

  if (typeof obj === "object") {
    for (const key in obj) {
      obj[key] = makeStringifyable(obj[key]);
    }
  }

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

function makeStringifyable(obj: any) {
  if (obj instanceof Error) {
    return {
      name: obj.name,
      message: obj.message,
      stack: obj.stack,
    };
  }

  return obj;
}
