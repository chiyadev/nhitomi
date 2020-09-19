/** btoa with unicode support. */
export function utoa(data: string) {
  return btoa(
    encodeURIComponent(data).replace(/%([0-9A-F]{2})/g, (_, p1) =>
      String.fromCharCode(parseInt(p1, 16))
    )
  );
}

/** atob with unicode support. */
export function atou(data: string) {
  return decodeURIComponent(
    atob(data)
      .split("")
      .map((c) => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2))
      .join("")
  );
}
