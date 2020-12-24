import React, { memo, ReactNode, useState } from "react";
import { chakra, HStack } from "@chakra-ui/react";
import ButtonItem from "./ButtonItem";
import { FaCog, FaFolder, FaSearch } from "react-icons/fa";
import SearchOverlay from "./SearchOverlay";
import LayoutBody from "../LayoutBody";
import StickyOverlay from "./StickyOverlay";
import { useT } from "../../locales";
import LinkItem from "./LinkItem";
import { useClientInfoAuth } from "../../utils/client";

const Header = ({ title, menu, shadow }: { title?: ReactNode; menu?: ReactNode; shadow?: boolean }) => {
  const t = useT();
  const info = useClientInfoAuth();
  const [search, setSearch] = useState(false);

  return (
    <>
      <SearchOverlay open={search} setOpen={setSearch} />

      <StickyOverlay shadow={shadow}>
        <LayoutBody p={4}>
          <HStack spacing={4} minH={6}>
            <chakra.div flex={1} minW={0}>
              {title}
            </chakra.div>

            {menu}

            <ButtonItem name={t("Header.search")} icon={FaSearch} onClick={() => setSearch(true)} />
            <LinkItem name={t("Header.collections")} icon={FaFolder} href={`/users/${info?.user.id}/collections`} />
            <LinkItem name={t("Header.settings")} icon={FaCog} href="/settings" />
          </HStack>
        </LayoutBody>
      </StickyOverlay>
    </>
  );
};

export default memo(Header);
