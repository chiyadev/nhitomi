import { Book, BookContent, DownloadSession } from "nhitomi-api";
import React, { memo, MutableRefObject, useEffect, useMemo, useState } from "react";
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
  const [progress, setProgress] = useState<
    | {
        type: "pending";
        indeterminate?: boolean;
      }
    | {
        type: "progress";
        value: number;
      }
  >({ type: "pending" });

  useEffect(() => {
    const client = createApiClient();

    if (!client) {
      return;
    }

    let proceed = true;
    let session: DownloadSession;

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
      setProgress({ type: "pending", indeterminate: true });

      try {
        const zip = new JSZip();
        const digits = content.pageCount.toString().length;

        // add book info
        zip.file("nhitomi.json", JSON.stringify(book, undefined, 2), {
          date: book.updatedTime,
        });

        let loaded = 0;

        // download images concurrently
        await PromiseEx.allN(
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
                const name = `${(i + 1).toString().padStart(digits, "0")}.${type}`;

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

        if (!proceed) {
          return;
        }

        setStatus("orange");
        setMessage(t("BookDownloader.Item.processing"));
        setProgress({ type: "progress", value: 0 });

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
          ({ percent }) => setProgress({ type: "progress", value: percent / 100 })
        );

        if (!proceed) {
          return;
        }

        setStatus("blue");
        setMessage(t("BookDownloader.Item.done"));
        setProgress({ type: "progress", value: 1 });

        saveAs(zipBlob, `${book.id} ${book.primaryName}.zip`);

        onComplete?.();
      } finally {
        clearInterval(keepalive);

        // delete session
        try {
          await client.download.deleteDownloadSession({ id: session.id });
        } catch {
          // ignored
        }
      }
    })();

    return () => {
      proceed = false;
    };
  }, []);

  return (
    <VStack align="stretch" spacing={2} opacity={progress.type === "pending" ? 0.5 : 1} transition="all .2s ease-out">
      <div>
        <Link href={`/books/${book.id}/contents/${content.id}`} isExternal>
          {book.primaryName}
        </Link>
      </div>

      {useMemo(() => {
        const style = {
          size: "xs",
          borderRadius: "md",
        };

        switch (progress.type) {
          case "pending":
            return <Progress {...style} colorScheme={status} isIndeterminate={progress.indeterminate} />;

          case "progress":
            return <Progress {...style} colorScheme={status} value={progress.value * 100} />;
        }
      }, [content, progress, status])}

      <chakra.div textAlign="center">{message}</chakra.div>
    </VStack>
  );
};

export default memo(ItemDisplay);
