import React, { createContext, memo, ReactNode } from "react";
import { Book, Collection, User } from "nhitomi-api";
import { useT } from "../../locales";
import { useClientInfoAuth } from "../../utils/client";
import Layout from "../Layout";
import Header from "../Header";
import HeaderTitle from "./HeaderTitle";
import HeaderMenu from "./HeaderMenu";
import LayoutBody from "../LayoutBody";
import BookGrid from "./Book/Grid";
import EmptyDisplay from "./EmptyDisplay";

export const CollectionMenuContext = createContext<{
  render: (collection: Collection) => ReactNode;
}>({
  render: () => undefined,
});

const CollectionListing = ({ user, books }: { user: User; books: { cover?: Book; collection: Collection }[] }) => {
  const t = useT();
  const info = useClientInfoAuth();

  return (
    <Layout
      title={[
        info?.user.id === user.id
          ? t("CollectionListing.title")
          : t("CollectionListing.titleUser", { user: user.username }),
      ]}
    >
      <Header back={info?.user.id !== user.id} title={<HeaderTitle user={user} />} menu={<HeaderMenu />} />

      <LayoutBody>{books.length ? <BookGrid items={books} /> : <EmptyDisplay />}</LayoutBody>
    </Layout>
  );
};

export default memo(CollectionListing);
