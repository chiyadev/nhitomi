import React, { Dispatch, memo } from "react";
import { Heading, HStack, Icon, Link, Radio, VStack } from "@chakra-ui/react";
import { useQuery } from "../../../utils/query";
import { SortDirection } from "nhitomi-api";
import { useT } from "../../../locales";
import { FaSortAlphaUp } from "react-icons/fa";
import NextLink from "next/link";
import { useRouter } from "next/router";

const OrderRadio = ({ setOpen }: { setOpen: Dispatch<boolean> }) => {
  const t = useT();
  const [current] = useQuery("order");

  const { query } = useRouter();

  return (
    <VStack align="start" spacing={4}>
      <Heading size="sm">
        <HStack spacing={2}>
          <Icon as={FaSortAlphaUp} />
          <div>{t("BookListing.QueryDrawer.OrderRadio.title")}</div>
        </HStack>
      </Heading>

      <VStack align="start" spacing={2}>
        {Object.values(SortDirection).map((order) => (
          <NextLink key={order} href={{ query: { ...query, order } }} passHref>
            <Link onClick={() => setOpen(false)}>
              <HStack>
                <Radio isChecked={order === current} />
                <div>{t("SortDirection", { value: order })}</div>
              </HStack>
            </Link>
          </NextLink>
        ))}
      </VStack>
    </VStack>
  );
};

export default memo(OrderRadio);
