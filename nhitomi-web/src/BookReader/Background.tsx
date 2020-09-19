import React from "react";
import { useClient } from "../ClientManager";
import { CoverImage } from "../Components/CoverImage";
import { css, cx } from "emotion";
import { useWindowScroll } from "../hooks";
import { animated, useSpring } from "react-spring";
import { PrefetchResult } from ".";
import { useConfig } from "../ConfigManager";
import { createPortal } from "react-dom";

export const Background = ({
  book,
  content,
  scrollHeight,
}: PrefetchResult & { scrollHeight: number }) => {
  const client = useClient();
  const { y: scroll } = useWindowScroll();

  const [blur] = useConfig("blur");

  const style = useSpring({
    opacity: Math.max(0, 1 - scroll / Math.max(1, scrollHeight)),
  });

  return createPortal(
    <animated.div
      style={style}
      className="fixed left-0 top-0 pointer-events-none"
    >
      <CoverImage
        cacheKey={`books/${book.id}/contents/${content.id}/pages/0`}
        className={cx(
          "w-screen h-screen",
          css`
            z-index: -1;
            opacity: ${blur ? "10%" : "5%"};
            filter: ${blur ? "blur(1em)" : "none"};
          `
        )}
        onLoad={async () =>
          await client.book.getBookImage({
            id: book.id,
            contentId: content.id,
            index: 0,
          })
        }
      />
    </animated.div>,
    document.body
  );
};
