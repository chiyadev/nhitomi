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
import { Client, useClient } from "./ClientManager";
import { useAsync } from "./hooks";
import { PromiseEx } from "./promiseEx";
import JSZip from "jszip";
import { probeImage } from "./imageUtils";

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
  const [targets, setTargets] = useConfig("downloads");
  const [tasks, setTasks] = useState<DownloadTask[]>([]);

  useLayoutEffect(
    () =>
      setTasks((tasks) => {
        // map targets to tasks
        const taskMap = createTaskMap(tasks);

        const newTasks = targets.map((target) => {
          const task = taskMap[target.id];

          if (task) return task;

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

  // handle task events
  const [proceed, setProceed] = useState(true);
  const handleUpdate = useCallback((task: DownloadTask) => {
    switch (task.state.type) {
      case "pending":
      case "done":
      case "error":
        setProceed(true);
        break;
    }
  }, []);

  // register task event handlers
  useLayoutEffect(() => {
    setProceed(true);

    for (const task of tasks) task.on("updated", handleUpdate);
    return () => {
      for (const task of tasks) task.off("updated", handleUpdate);
    };
  }, [tasks]);

  // assign download sessions to tasks
  const proceeding = useRef(false);

  useAsync(async () => {
    if (!proceed) return;
    setProceed(false);

    if (proceeding.current) return;
    proceeding.current = true;

    try {
      for (const task of tasks) {
        if (task.state.type !== "pending") continue;

        try {
          const session = await client.download.createDownloadSession({ body: {} });

          task.setProgress("download", 0);
          task.run(client, session, (task.cancellation = { requested: false }));
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

          add: (...addTargets) => {
            setTargets((targets) => [
              ...addTargets.map((target) => ({ ...target, id: Math.random(), owner: currentOwnerId })),
              ...targets,
            ]);
          },

          remove: (...ids) => {
            setTargets((targets) => targets.filter((target) => ids.indexOf(target.id) === -1));
          },
        }),
        [targets, tasks]
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
      result: Blob;
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
    if (this.cancellation) {
      this.cancellation.requested = true;
      this.cancellation = undefined;
    }

    this.emit("updated", this);
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

  setResult(result: Blob) {
    this.state = { type: "done", result };
    this.progress = 1;

    this.emit("updated", this);
  }

  setError(error: Error) {
    this.state = { type: "error", error };

    this.emit("updated", this);
  }

  async run(client: Client, session: DownloadSession, cancellation: CancellationSignal) {
    let resultFn: (() => Promise<Blob>) | undefined;

    try {
      const result = await this.runCore(client, session, cancellation);

      if (cancellation.requested || !result) return;
      resultFn = result;
    } catch (e) {
      if (!cancellation.requested) this.setError(e);
    } finally {
      try {
        await client.download.deleteDownloadSession({ id: session.id });
      } catch {
        // ignored
      }
    }

    if (!resultFn) return;

    // result generation can be run outside session
    try {
      const result = await resultFn();

      if (!cancellation.requested) this.setResult(result);
    } catch (e) {
      if (!cancellation.requested) this.setError(e);
    }
  }

  abstract runCore(
    client: Client,
    session: DownloadSession,
    cancellation: CancellationSignal
  ): Promise<(() => Promise<Blob>) | undefined>;
}

class BookDownloadTask extends DownloadTask {
  constructor(target: DownloadTarget) {
    super(target);
  }

  async runCore(client: Client, session: DownloadSession, cancellation: CancellationSignal) {
    if (this.target.type !== "book") throw Error("Target must be book.");
    const { id, contentId } = this.target.book;

    const book = await client.book.getBook({ id });
    const content = book.contents.find((c) => c.id === contentId);

    if (!content) throw Error(`'${contentId}' not found.`);
    if (cancellation.requested) return;

    const digits = content.pageCount.toString().length;
    const zip = new JSZip();
    let loaded = 0;

    await PromiseEx.allN(
      session.concurrency,
      Array.from(Array(content.pageCount).keys()).map((index) => async () => {
        // retry failed requests indefinitely
        // user can cancel the download if deemed stuck
        while (!cancellation.requested) {
          try {
            const image = await client.book.getBookImage({ id, contentId, index, sessionId: session.id });
            const { type } = await probeImage(image);

            if (cancellation.requested) return;

            zip.file(`${(index + 1).toString().padStart(digits, "0")}.${type}`);
            this.setProgress("download", ++loaded / content.pageCount);

            return;
          } catch (e) {
            console.warn("could not load image", index, "of book", id, contentId, e);
          }
        }
      })
    );

    if (cancellation.requested) return;

    return async () =>
      await zip.generateAsync(
        {
          // https://stuk.github.io/jszip/documentation/api_jszip/generate_async.html
          type: "blob",
          comment: `Downloaded from nhitomi on ${new Date().toString()} (/books/${id}/contents/${contentId}).`,
          compression: "DEFLATE",
          compressionOptions: {
            level: 9,
          },
        },
        ({ percent }) => this.setProgress("process", percent / 100)
      );
  }
}
