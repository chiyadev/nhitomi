import { Book, BookContent, DownloadSession } from "nhitomi-api";
import React, { memo, MutableRefObject, useEffect, useState } from "react";
import { chakra, Link, Progress, VStack } from "@chakra-ui/react";
import JSZip from "jszip";
import { createApiClient } from "../../utils/client";
import { useT } from "../../locales";
import { PromiseEx } from "../../utils/promises";
import { probeImage } from "../../utils/image";
import { saveAs } from "file-saver";

function sleep(ms: number) {
  return new Promise<void>((resolve) => setTimeout(resolve, ms));
}

const ItemDisplay = ({
  book,
  content,
  index,
  sessions,
  onComplete,
}: {
  book: Book;
  content: BookContent;
  index: number;
  sessions: MutableRefObject<number>;
  onComplete?: () => void;
}) => {
  const t = useT();
  const [status, setStatus] = useState("blue");
  const [message, setMessage] = useState("");
  const [progress, setProgress] = useState<{
    type: "pending" | "progress";
    value?: number;
    indeterminate?: boolean;
  }>({ type: "pending" });

  useEffect(() => {
    const client = createApiClient();

    let proceed = true;
    let session: DownloadSession;

    const onUnmount = new Set<() => void>();

    (async () => {
      while (true) {
        // create download sessions one at a time
        if (proceed && sessions.current === index) {
          try {
            session = await client.download.createDownloadSession({ body: {} });
            sessions.current++;

            break;
          } catch {
            // wait for other downloads to complete
          }
        }

        if (proceed) {
          await sleep(1000);
        } else {
          return;
        }
      }

      // begin keepalive
      const keepalive = window.setInterval(async () => {
        try {
          await client.download.getDownloadSession({ id: session.id });
        } catch {
          proceed = false;
        }
      }, 10000);

      setStatus("blue");
      setProgress({ type: "progress", indeterminate: true });

      const zip = new JSZip();

      try {
        // add book info
        zip.file("nhitomi.json", JSON.stringify(book, undefined, 2), {
          date: book.updatedTime,
        });

        let loaded = 0;

        // download images concurrently
        const imagesPromise = PromiseEx.allN(
          session.concurrency,
          Array.from(Array(content.pageCount).keys()).map((i) => async () => {
            for (let retry = 0; proceed; retry++) {
              try {
                // download using session
                const imageBlob = await client.book.getBookImage({
                  id: book.id,
                  contentId: content.id,
                  index: i,
                  sessionId: session.id,
                });

                // name image with zero padding
                const { type } = probeImage(await new Response(imageBlob).arrayBuffer());
                const name = `${(i + 1).toString().padStart(content.pageCount.toString().length, "0")}.${type}`;

                // add image to zip
                zip.file(name, imageBlob, {
                  binary: true,
                  date: book.updatedTime,
                });

                setMessage(t("BookDownloader.Item.downloaded", { name }));
                setProgress({ type: "progress", value: ++loaded / content.pageCount });

                return;
              } catch (e) {
                console.error(e);

                // retry failure indefinitely with linear backoff
                await new Promise((resolve) => setTimeout(resolve, 1000 * Math.min(retry, session.concurrency)));
              }
            }
          })
        );

        // exit immediately to delete session if unmounted before download completes
        await Promise.race([imagesPromise, new Promise((resolve) => onUnmount.add(resolve))]);

        if (!proceed) {
          return;
        }

        setStatus("orange");
        setMessage(t("BookDownloader.Item.processing"));
        setProgress({ type: "progress", indeterminate: true });
      } catch (e) {
        setStatus("red");
        setMessage(e.message);
        return;
      } finally {
        // stop keepalive
        clearInterval(keepalive);

        // delete session
        try {
          await client.download.deleteDownloadSession({ id: session.id });
        } catch {
          // ignored
        }
      }

      try {
        const readerPath = `/books/${book.id}/contents/${content.id}`;

        const zipBlob = await zip.generateAsync(
          {
            type: "blob",
            comment: `Downloaded from nhitomi on ${new Date().toISOString()} (${readerPath}).`,
            compression: "DEFLATE",
            compressionOptions: {
              level: 9,
            },
          },
          ({ percent, currentFile }) => {
            setMessage(currentFile);
            setProgress({ type: "progress", value: percent / 100 });
          }
        );

        if (!proceed) {
          return;
        }

        setStatus("blue");
        setMessage(t("BookDownloader.Item.done"));
        setProgress({ type: "progress", value: 1 });

        saveAs(zipBlob, `${book.id} ${book.primaryName}.zip`);

        onComplete?.();
      } catch (e) {
        setStatus("red");
        setMessage(e.message);
        return;
      }
    })();

    return () => {
      proceed = false;

      for (const callback of onUnmount) {
        onUnmount.delete(callback) && callback();
      }
    };
  }, []);

  return (
    <VStack align="stretch" spacing={2} opacity={progress.type === "pending" ? 0.5 : 1} transition="all .2s ease-out">
      <div>
        <Link href={`/books/${book.id}/contents/${content.id}`} isExternal>
          {book.primaryName}
        </Link>
      </div>

      <Progress
        size="xs"
        borderRadius="full"
        colorScheme={status}
        value={progress.value ? progress.value * 100 : undefined}
        isIndeterminate={progress.indeterminate}
      />

      <chakra.div textAlign="center">{message}</chakra.div>
    </VStack>
  );
};

export default memo(ItemDisplay);
