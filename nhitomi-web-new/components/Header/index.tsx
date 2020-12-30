import React, { memo, ReactNode, useState } from "react";
import { chakra, Divider, HStack, IconButton, Icon } from "@chakra-ui/react";
import ButtonItem from "./ButtonItem";
import { FaChevronLeft, FaCog, FaFolder, FaHeart, FaSearch } from "react-icons/fa";
import SearchOverlay from "./SearchOverlay";
import LayoutBody from "../LayoutBody";
import StickyOverlay from "./StickyOverlay";
import { useT } from "../../locales";
import LinkItem from "./LinkItem";
import { useClientInfoAuth } from "../../utils/client";
import { trackEvent } from "../../utils/umami";
import { useRouter } from "next/router";

const Header = ({
  title,
  menu,
  shadow,
  back,
}: {
  title?: ReactNode;
  menu?: ReactNode;
  shadow?: boolean;
  back?: boolean;
}) => {
  const t = useT();
  const info = useClientInfoAuth();
  const router = useRouter();
  const [search, setSearch] = useState(false);

  return (
    <>
      <SearchOverlay open={search} setOpen={setSearch} />

      <StickyOverlay shadow={shadow}>
        <LayoutBody>
          <HStack spacing={0}>
            {back && (
              <IconButton
                aria-label="Back"
                variant="ghost"
                icon={<Icon as={FaChevronLeft} />}
                ml={2}
                onClick={router.back}
              />
            )}

            <HStack flex={1} spacing={4} minW={0} minH={6} p={4}>
              <chakra.div flex={1} minW={0}>
                {title}
              </chakra.div>

              {menu && (
                <>
                  {menu}
                  <Divider orientation="vertical" h={4} />
                </>
              )}

              <ButtonItem
                name={t("Header.search")}
                icon={FaSearch}
                onClick={() => {
                  setSearch(true);
                  trackEvent("header", "search");
                }}
              />

              <LinkItem name={t("Header.collections")} icon={FaFolder} href={`/users/${info?.user.id}/collections`} />
              <LinkItem name={t("Header.support")} icon={FaHeart} href="/support" />
              <LinkItem name={t("Header.settings")} icon={FaCog} href="/settings" />
            </HStack>
          </HStack>
        </LayoutBody>
      </StickyOverlay>
    </>
  );
};

export default memo(Header);
