import React, { Dispatch, memo, SetStateAction, useCallback, useMemo } from "react";
import { Book, BookContent } from "nhitomi-api";
import BookImage from "../../../BookImage";
import { ImageInfo, LayoutImage } from "./layoutEngine";
import { probeImage } from "../../../../utils/image";

const Page = ({
  book,
  content,
  index,
  image,
  setImage,
}: {
  book: Book;
  content: BookContent;
  index: number;
  image: LayoutImage;
  setImage: Dispatch<SetStateAction<ImageInfo | undefined>>;
}) => {
  return (
    <BookImage
      position="absolute"
      book={book}
      content={content}
      index={index}
      intersection={useMemo(() => ({ rootMargin: "100%" }), [])}
      style={useMemo(
        () => ({
          left: image.x,
          top: image.y,
          width: image.width,
          height: image.height,
        }),
        [image.x, image.y, image.width, image.height]
      )}
      onLoaded={useCallback(
        async (data: Blob) => {
          const result = probeImage(await new Response(data).arrayBuffer());

          if (result) {
            setImage(result);
          } else {
            throw Error(`Could not detect image dimensions for ${book.id}/${content.id}/${index}.`);
          }
        },
        [setImage]
      )}
    />
  );
};

export default memo(Page);
