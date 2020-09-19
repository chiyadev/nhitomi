if (process.env.NODE_ENV === "production") {
  const noop = (): void => {
    /* nothing */
  };

  console.debug = noop;
  console.info = noop;
  console.log = noop;
}
