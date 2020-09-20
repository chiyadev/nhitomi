import { DownloadTask, useDownloads } from "../DownloadManager";
import { useClient } from "../ClientManager";
import { useUpdate } from "react-use";
import React, { useLayoutEffect, useMemo, useState } from "react";
import { useConfig } from "../ConfigManager";
import { animated, useSpring } from "react-spring";
import { BookReaderLink } from "../BookReader";
import { CoverImage } from "../Components/CoverImage";
import { FormattedMessage } from "react-intl";
import { Tooltip } from "../Components/Tooltip";
import { CloseOutlined, ReloadOutlined } from "@ant-design/icons";
import { cx } from "emotion";
import { useNotify } from "../NotificationManager";
import VisibilitySensor from "react-visibility-sensor";

export const BookTaskDisplay = ({ task }: { task: DownloadTask }) => {
  const target = task.target;
  const client = useClient();

  const update = useUpdate();
  useLayoutEffect(() => {
    task.on("updated", update);
    return () => {
      task.off("update", update);
    };
  }, [task]);

  const { remove } = useDownloads();
  const { notifyError } = useNotify();
  const [preferEnglishName] = useConfig("bookReaderPreferEnglishName");

  const [visible, setVisible] = useState(false);
  const [hover, setHover] = useState(false);

  const style = useSpring({
    opacity: hover || task.active ? 1 : 0.5,
  });

  const buttonStyle = useSpring({
    opacity: hover ? 1 : 0,
  });

  if (target.type !== "book") return null;
  const { book } = target;

  return (
    <VisibilitySensor partialVisibility offset={{ top: -400, bottom: -400 }} onChange={setVisible}>
      {visible ? (
        <animated.div
          style={style}
          className="w-full m-2 p-2 flex flex-row relative"
          onMouseEnter={() => setHover(true)}
          onMouseLeave={() => setHover(false)}
        >
          {useMemo(
            () => (
              <BookReaderLink id={book.id} contentId={book.contentId}>
                <CoverImage
                  className="w-24 h-32"
                  cacheKey={`books/${book.id}/contents/${book.contentId}/pages/-1`}
                  onLoad={async () => await client.book.getBookImage({ ...book, index: -1 })}
                />
              </BookReaderLink>
            ),
            [client, book]
          )}

          <div className="pl-2 flex-1 flex flex-col">
            {useMemo(
              () => (
                <div>
                  <BookReaderLink id={book.id} contentId={book.contentId}>
                    <div className="text-lg font-bold">
                      {(preferEnglishName && book.englishName) || book.primaryName}
                    </div>
                    <div className="text-sm text-gray-darker">
                      {(!preferEnglishName && book.englishName) || book.primaryName}
                    </div>
                  </BookReaderLink>
                </div>
              ),
              [book]
            )}

            {useMemo(
              () => (
                <div className="flex-1" />
              ),
              []
            )}

            {useMemo(() => {
              let labelKey = `pages.downloads.task.state.${task.state.type}`;
              const error = task.state.type === "error" && task.state.error;

              if (task.state.type === "running") labelKey = `${labelKey}.${task.state.stage}`;

              return (
                <>
                  <div
                    className={cx("text-sm text-gray-darker", {
                      "text-green": task.state.type === "done",
                      "text-red-darker": !!error,
                    })}
                  >
                    <FormattedMessage id={labelKey} />
                    {" — "}
                    {Math.round(task.progress * 100)}%
                  </div>

                  {error && (
                    <div className="text-sm text-gray-darker cursor-pointer" onClick={() => notifyError(error)}>
                      {error.message}
                    </div>
                  )}
                </>
              );
            }, [task.state, task.progress])}
          </div>

          <animated.div
            style={buttonStyle}
            className="absolute top-0 right-0 flex flex-row bg-gray-darkest bg-blur rounded"
          >
            {(!task.active || task.state.type === "error") && (
              <Tooltip overlay={<FormattedMessage id="pages.downloads.task.restart" />} placement="bottom">
                <div className="text-gray-darker text-sm cursor-pointer p-2" onClick={() => task.restart()}>
                  <ReloadOutlined />
                </div>
              </Tooltip>
            )}

            <Tooltip overlay={<FormattedMessage id="pages.downloads.task.delete" />} placement="bottom">
              <div className="text-gray-darker text-sm cursor-pointer p-2" onClick={() => remove(target.id)}>
                <CloseOutlined />
              </div>
            </Tooltip>
          </animated.div>
        </animated.div>
      ) : (
        <div className="w-full m-4 h-32" />
      )}
    </VisibilitySensor>
  );
};
