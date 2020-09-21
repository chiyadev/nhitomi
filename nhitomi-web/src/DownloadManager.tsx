import React, {
  createContext,
  ReactNode,
  useCallback,
  useContext,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { useConfig } from "./ConfigManager";
import { EventEmitter } from "events";
import StrictEventEmitter from "strict-event-emitter-types";
import { Book, DownloadSession } from "nhitomi-api";
import { Client, ClientInfo, useClient, useClientInfo } from "./ClientManager";
import { useAsync } from "./hooks";
import { PromiseEx } from "./promiseEx";
import JSZip from "jszip";
import { probeImage } from "./imageUtils";
import { saveAs } from "file-saver";
import stringify from "json-stable-stringify";

// download tasks are "owned" by a specific instance/tab of nhitomi
// sessionStorage is not used to store this. we want a new id every time an instance loads.
const currentOwnerId = Math.random();

export type DownloadTarget = DownloadTargetAddArgs & {
  id: number;
  owner: number;
};

type DownloadTargetAddArgs = {
  type: "book";
  book: Pick<Book, "primaryName" | "englishName"> & {
    id: string;
    contentId: string;
  };
};

const DownloadContext = createContext<{
  targets: DownloadTarget[];
  tasks: DownloadTask[];

  add: (...targets: DownloadTargetAddArgs[]) => void;
  remove: (...ids: number[]) => void;
}>(undefined as any);

export function useDownloads() {
  return useContext(DownloadContext);
}

function createTaskMap(tasks: DownloadTask[]) {
  return tasks.reduce((x, t) => {
    x[t.id] = t;
    return x;
  }, {} as Record<number, DownloadTask>);
}

export const DownloadManager = ({ children }: { children: ReactNode }) => {
  const client = useClient();
  const { info } = useClientInfo();

  const [targets, setTargets] = useConfig("downloads");
  const [tasks, setTasks] = useState<DownloadTask[]>([]);

  const add = useCallback(
    (...addTargets: DownloadTargetAddArgs[]) => {
      setTargets((targets) => [
        ...addTargets
          .filter(
            // prevent duplicates
            (target) =>
              targets.findIndex((other) => {
                if (target.type !== other.type) return false;

                switch (target.type) {
                  case "book":
                    return target.book.id === other.book.id && target.book.contentId === other.book.contentId;
                }
              }) === -1
          )
          .map((target) => ({ ...target, id: Math.random(), owner: currentOwnerId })),
        ...targets,
      ]);
    },
    [setTargets]
  );

  const remove = useCallback(
    (...ids: number[]) => {
      setTargets((targets) => targets.filter((target) => ids.indexOf(target.id) === -1));
    },
    [setTargets]
  );

  useLayoutEffect(
    () =>
      setTasks((tasks) => {
        // map targets to tasks
        const taskMap = createTaskMap(tasks);

        const newTasks = targets.map((target) => {
          const task = taskMap[target.id];
          if (task) return task;

          // new task added, proceed
          setProceed(true);

          switch (target.type) {
            case "book":
              return new BookDownloadTask(target);
          }
        });

        // abort all removed tasks
        const newTaskMap = createTaskMap(newTasks);

        for (const task of tasks) {
          if (!newTaskMap[task.id]) task.cancel();
        }

        return newTasks;
      }),
    [targets]
  );

  const [proceed, setProceed] = useState(true);
  const proceeding = useRef(false);

  useLayoutEffect(() => {
    const handle = (task: DownloadTask) => {
      switch (task.state.type) {
        // a task became pending, proceed
        case "pending":
          setProceed(true);
          break;

        // a task completed successfully, so save and remove
        case "done":
          setProceed(true);
          remove(task.id);

          saveAs(task.state.data, task.state.name);
          break;
      }
    };

    for (const task of tasks) task.on("updated", handle);
    return () => {
      for (const task of tasks) task.off("updated", handle);
    };
  }, [tasks]);

  useAsync(async () => {
    if (!proceed) return;
    setProceed(false);

    // prevent concurrent session creation
    if (proceeding.current) return;
    proceeding.current = true;

    try {
      for (const task of tasks) {
        if (task.state.type !== "pending") continue;

        try {
          // create a session for each pending download
          const session = await client.download.createDownloadSession({ body: {} });

          task.setProgress("download", 0);

          // run task with cancellation in the background
          // when the task finishes, regardless of success, we will proceed to the next task
          task.run(client, info, session, (task.cancellation = { requested: false })).finally(() => setProceed(true));
        } catch {
          break;
        }
      }
    } finally {
      proceeding.current = false;
    }
  }, [proceed, tasks]);

  return (
    <DownloadContext.Provider
      value={useMemo(
        () => ({
          targets,
          tasks,
          add,
          remove,
        }),
        [targets, tasks, add, remove]
      )}
    >
      {children}
    </DownloadContext.Provider>
  );
};

// stalled: not scheduled to run
// pending: scheduled to run but is waiting for other tasks to complete
// running: task is running
// error:   task failed
// done:    task succeeded
type TaskState =
  | {
      type: "stalled" | "pending";
    }
  | {
      type: "running";
      stage: TaskRunningStage;
    }
  | {
      type: "error";
      error: Error;
    }
  | {
      type: "done";
      data: Blob;
      name: string;
    };

type TaskRunningStage = "download" | "process";
type CancellationSignal = { requested: boolean };

export abstract class DownloadTask extends (EventEmitter as new () => StrictEventEmitter<
  EventEmitter,
  { updated: (task: DownloadTask) => void }
>) {
  get id() {
    return this.target.id;
  }

  get owned() {
    return this.target.owner === currentOwnerId;
  }

  get active() {
    switch (this.state.type) {
      case "running":
      case "error":
      case "done":
        return true;
    }

    return false;
  }

  state: TaskState;
  progress = 0;

  protected constructor(readonly target: DownloadTarget) {
    super();

    if (this.owned) this.state = { type: "pending" };
    else this.state = { type: "stalled" };
  }

  cancellation?: CancellationSignal;

  cancel() {
    const cancellation = this.cancellation;

    if (cancellation) {
      cancellation.requested = true;
      this.cancellation = undefined;
    }
  }

  restart() {
    this.cancel();

    this.state = { type: "pending" };
    this.progress = 0;

    this.emit("updated", this);
  }

  setProgress(stage: TaskRunningStage, progress: number) {
    this.state = { type: "running", stage };
    this.progress = progress;

    this.emit("updated", this);
  }

  setResult(data: Blob, name: string) {
    this.state = { type: "done", data, name };
    this.progress = 1;

    this.emit("updated", this);
  }

  setError(error: Error) {
    this.state = { type: "error", error };

    this.emit("updated", this);
  }

  async run(client: Client, info: ClientInfo, session: DownloadSession, cancellation: CancellationSignal) {
    // interval to keep session alive
    const sessionInterval = window.setInterval(
      async () => (session = await client.download.getDownloadSession({ id: session.id })),
      10000
    );

    let resultFn: ResultBlobGenerator | undefined;

    try {
      const result = await this.runCore(client, info, session, cancellation);

      if (cancellation.requested || !result) return;
      resultFn = result;
    } catch (e) {
      if (!cancellation.requested) this.setError(e);
    } finally {
      clearInterval(sessionInterval);

      try {
        await client.download.deleteDownloadSession({ id: session.id });
      } catch {
        // ignored
      }
    }

    if (!resultFn) return;

    // result generation can be run outside session
    try {
      const { data, name } = await resultFn();

      if (!cancellation.requested) this.setResult(data, name);
    } catch (e) {
      if (!cancellation.requested) this.setError(e);
    }
  }

  abstract runCore(
    client: Client,
    info: ClientInfo,
    session: DownloadSession,
    cancellation: CancellationSignal
  ): Promise<ResultBlobGenerator | undefined>;
}

type ResultBlobGenerator = () => Promise<{ data: Blob; name: string }>;

class BookDownloadTask extends DownloadTask {
  constructor(target: DownloadTarget) {
    super(target);
  }

  async runCore(client: Client, info: ClientInfo, session: DownloadSession, cancellation: CancellationSignal) {
    if (this.target.type !== "book") throw Error("Target must be book.");
    const { id, contentId } = this.target.book;

    const book = await client.book.getBook({ id });
    const content = book.contents.find((c) => c.id === contentId);

    if (cancellation.requested) return;
    if (!content) throw Error(`'${contentId}' not found.`);

    const digits = content.pageCount.toString().length;
    const zip = new JSZip();

    // add information files
    zip.file("nhitomi.json", stringify(book, { space: 2 }));
    zip.file(
      "sources.json",
      stringify(
        {
          [content.source]: content.sourceUrl,
          nhitomi: `${info.publicUrl}/books/${id}/contents/${contentId}`,
        },
        { space: 2 }
      )
    );

    let loaded = 0;

    await PromiseEx.allN(
      session.concurrency,
      Array.from(Array(content.pageCount).keys()).map((index) => async () => {
        // retry failed requests indefinitely
        // user can cancel the download if deemed stuck
        while (!cancellation.requested) {
          try {
            // download image using session
            const image = await client.book.getBookImage({ id, contentId, index, sessionId: session.id });
            const { type } = await probeImage(image);

            if (cancellation.requested) return;

            // add image to zip
            zip.file(`${(index + 1).toString().padStart(digits, "0")}.${type}`, image, {
              binary: true,
              date: book.updatedTime,
            });

            // update progress
            this.setProgress("download", ++loaded / content.pageCount);

            return; // exit loop
          } catch (e) {
            console.warn("could not load image", index, "of book", id, contentId, e);
          }
        }
      })
    );

    if (cancellation.requested) return;

    return async () => ({
      data: await zip.generateAsync(
        // https://stuk.github.io/jszip/documentation/api_jszip/generate_async.html
        {
          type: "blob",
          comment: `Downloaded from nhitomi on ${new Date().toString()} (/books/${id}/contents/${contentId}).`,
          compression: "DEFLATE",
          compressionOptions: {
            level: 9,
          },
        },
        ({ percent }) => this.setProgress("process", percent / 100)
      ),
      name: `${id} ${book.primaryName}.zip`,
    });
  }
}
