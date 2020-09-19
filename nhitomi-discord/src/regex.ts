export class RegExpCache {
  static readonly flags = "gi";
  static readonly cache: { [key: string]: RegExp } = {};

  static get(pattern: string): RegExp {
    const cache = this.cache[pattern];

    return cache || (this.cache[pattern] = new RegExp(pattern, this.flags));
  }
}
