import React, { Dispatch, memo } from "react";
import { Heading, HStack, Icon, Link, Radio, VStack } from "@chakra-ui/react";
import { useQuery } from "../../../utils/query";
import { BookSort } from "nhitomi-api";
import { useT } from "../../../locales";
import { FaSortAmountUp } from "react-icons/fa";
import NextLink from "next/link";
import { useRouter } from "next/router";
import { trackEvent } from "../../../utils/umami";

const SortRadio = ({ setOpen }: { setOpen: Dispatch<boolean> }) => {
  const t = useT();
  const [current] = useQuery("sort");

  const { query } = useRouter();

  return (
    <VStack align="start" spacing={4}>
      <Heading size="sm">
        <HStack spacing={2}>
          <Icon as={FaSortAmountUp} />
          <div>{t("BookListing.QueryDrawer.SortRadio.title")}</div>
        </HStack>
      </Heading>

      <VStack align="start" spacing={2}>
        {Object.values(BookSort).map((sort) => (
          <NextLink key={sort} href={{ query: { ...query, sort } }} passHref>
            <Link
              onClick={() => {
                setOpen(false);
                trackEvent("bookListing", `sort${sort}`);
              }}
            >
              <HStack>
                <Radio isChecked={sort === current} />
                <div>{t("BookSort", { value: sort })}</div>
              </HStack>
            </Link>
          </NextLink>
        ))}
      </VStack>
    </VStack>
  );
};

export default memo(SortRadio);
