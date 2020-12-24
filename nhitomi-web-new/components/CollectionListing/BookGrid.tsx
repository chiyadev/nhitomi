import React, { memo, useMemo } from "react";
import { Book, Collection } from "nhitomi-api";
import { Grid } from "@chakra-ui/react";
import styles from "../BookGrid/index.module.css";
import BookItem from "./BookItem";

const BookGrid = ({ items }: { items: { cover?: Book; collection: Collection }[] }) => {
  return (
    <Grid p={2} gap={2} className={styles.grid}>
      {useMemo(
        () =>
          items.map(({ cover, collection }) => <BookItem key={collection.id} cover={cover} collection={collection} />),
        [items]
      )}
    </Grid>
  );
};

export default memo(BookGrid);
