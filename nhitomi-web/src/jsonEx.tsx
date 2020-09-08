const dateRegex = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2}(?:\.\d*))(?:Z|(\+|-)([\d|:]*))?$/

/** Better JSON!!! */
export class JSONex {
  /** Similar to JSON.parse but adds support for reviving Dates. */
  static parse(text: string) {
    return JSON.parse(text, (_, value) => {
      // https://weblog.west-wind.com/posts/2014/jan/06/javascript-json-date-parsing-and-real-dates
      if (typeof value === 'string' && value.match(dateRegex))
        return new Date(value)

      return value
    })
  }
}
