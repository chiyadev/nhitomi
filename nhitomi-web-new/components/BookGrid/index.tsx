import React, { memo, useMemo } from "react";
import { Book } from "nhitomi-api";
import { Grid } from "@chakra-ui/react";
import styles from "./index.module.css";
import Item from "./Item";

const BookGrid = ({ items }: { items: Book[] }) => {
  return (
    <Grid p={2} gap={2} className={styles.grid}>
      {useMemo(() => items.map((book) => <Item key={book.id} book={book} />), [items])}
    </Grid>
  );
};

export default memo(BookGrid);
