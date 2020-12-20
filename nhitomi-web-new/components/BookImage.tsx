import React, { ComponentProps, memo, useEffect, useRef, useState } from "react";
import { Book, BookContent } from "nhitomi-api";
import { Center, chakra, Fade, Icon, Spinner } from "@chakra-ui/react";
import { useBlobUrl } from "../utils/hooks";
import { IntersectionOptions, useInView } from "react-intersection-observer";
import { createApiClient } from "../utils/client";
import TransparentPixel from "../assets/TransparentPixel.png";

const BookImage = ({
  className,
  book,
  content,
  index,
  animateIn = "fade",
  intersection,
  onLoaded,
  ...props
}: {
  className?: string;
  book: Book;
  content: BookContent;
  index: number;
  animateIn?: "fade" | "scale" | "none";
  intersection?: IntersectionOptions;
  onLoaded?: (data: Blob) => void | Promise<void>;
} & ComponentProps<typeof chakra.img>) => {
  const loadId = useRef(0);
  const [ref, visible] = useInView({
    ...intersection,
    triggerOnce: true,
  });

  const [result, setResult] = useState<Blob | Error>();
  const [loading, setLoading] = useState(false);
  const src = useBlobUrl(typeof window !== "undefined" && result instanceof Blob ? result : undefined);

  const [animateProps, setAnimateProps] = useState(() => {
    switch (animateIn) {
      case "none":
        return {};

      case "fade":
        return { opacity: 0 };

      case "scale":
        return { opacity: 0, transform: "scale(0.95)" };
    }
  });

  useEffect(() => {
    if (!visible) return;

    (async () => {
      const loadingTimeout = setTimeout(() => setLoading(true), 3000);

      try {
        const id = ++loadId.current;
        const client = createApiClient();

        if (client) {
          const data = await client.book.getBookImage({
            id: book.id,
            contentId: content.id,
            index,
          });

          await onLoaded?.(data);

          if (id === loadId.current) {
            setLoading(false);
            setResult(data);
            setTimeout(() => setAnimateProps({}));
          }
        } else {
          setResult(Error("Unauthorized."));
        }
      } catch (e) {
        setResult(e);
      } finally {
        clearTimeout(loadingTimeout);
      }
    })();
  }, [visible, book.id, content.id, index]);

  if (loading) {
    return (
      <Center {...props}>
        <Fade in>
          <Icon as={Spinner} />
        </Fade>
      </Center>
    );
  }

  return (
    <chakra.img
      ref={ref}
      alt={`${book.id}/${content.id}/${index}`}
      src={src || TransparentPixel}
      transition="all 0.6s cubic-bezier(0.16, 1, 0.3, 1)"
      transitionProperty="opacity, transform"
      {...animateProps}
      {...props}
    />
  );
};

export default memo(BookImage);
