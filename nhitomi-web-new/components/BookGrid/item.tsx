import React, { memo } from "react";
import { Book } from "nhitomi-api";

const Item = ({ book }: { book: Book }) => {
  return <div>{book.primaryName}</div>;
};

export default memo(Item);
