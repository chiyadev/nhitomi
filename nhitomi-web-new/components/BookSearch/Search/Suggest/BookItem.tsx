import React, { memo } from "react";
import { Book } from "nhitomi-api";

const BookItem = ({ book: { id, primaryName } }: { book: Book }) => {
  return (
    <div>
      {id} {primaryName}
    </div>
  );
};

export default memo(BookItem);
