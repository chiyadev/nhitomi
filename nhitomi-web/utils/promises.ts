export class PromiseEx {
  /** https://gist.github.com/jcouyang/632709f30e12a7879a73e9e132c0d56b#gistcomment-3253738 */
  static async allN<T>(limit: number, promises: (() => Promise<T>)[]) {
    const head = promises.slice(0, limit);
    const tail = promises.slice(limit);
    const result: T[] = [];

    const execute = async (promise: () => Promise<T>, i: number, runNext: () => Promise<void>) => {
      result[i] = await promise();
      await runNext();
    };

    const runNext = async () => {
      const i = promises.length - tail.length;
      const promise = tail.shift();
      if (promise !== undefined) {
        await execute(promise, i, runNext);
      }
    };

    await Promise.all(head.map((promise, i) => execute(promise, i, runNext)));
    return result;
  }
}
