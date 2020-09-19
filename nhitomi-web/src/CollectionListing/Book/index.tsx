import React, { useCallback, useMemo } from "react";
import { BookCollection } from "..";
import { BookList, BookListItem } from "../../Components/BookList";
import { BookContent, User } from "nhitomi-api";
import { CollectionContentLink } from "../../CollectionContent";
import { FormattedMessage } from "react-intl";
import { EmptyIndicator } from "../../Components/EmptyIndicator";
import { Menu } from "./Menu";
import { Overlay } from "./Overlay";

// instead of reimplementing a new list for book collections, adapt BookList for code reuse
export const BookSection = ({ user, collections }: { user: User, collections: BookCollection[] }) => {
  const items = useMemo(() => collections.map(({ collection, cover }) => ({
    ...cover || { contents: [] },

    id: collection.id, // use collection id instead of cover id
    primaryName: collection.name,
    englishName: collection.description
  })), [collections]);

  const getCoverRequest = useCallback((book: BookListItem, content: BookContent) => ({
    id: collections.find(c => c.collection.id === book.id)?.cover?.id!, // convert collection id to cover id
    contentId: content.id,
    index: -1
  }), [collections]);

  return useMemo(() => (
    <BookList
      items={items}
      preferEnglishName={false} // preferEnglishName would swap collection name and description
      overlayVisible
      getCoverRequest={getCoverRequest}
      menu={(
        <Menu />
      )}
      empty={(
        <EmptyIndicator>
          <FormattedMessage id='pages.collectionListing.empty' />
        </EmptyIndicator>
      )}
      LinkComponent={CollectionContentLink}
      OverlayComponent={props => (
        <Overlay user={user} {...props} />
      )} />
  ), [getCoverRequest, items, user]);
};
