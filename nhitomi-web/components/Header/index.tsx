import React, { memo, ReactNode } from "react";
import { chakra, Divider, HStack, Icon, IconButton } from "@chakra-ui/react";
import { FaBook, FaChevronLeft, FaCog, FaFolder, FaHeart } from "react-icons/fa";
import LayoutBody from "../LayoutBody";
import StickyOverlay from "./StickyOverlay";
import { useT } from "../../locales";
import LinkItem from "./LinkItem";
import { useClientInfoAuth } from "../../utils/client";
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

  return (
    <>
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

              <LinkItem name={t("Header.books")} icon={FaBook} href="/books" />
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
