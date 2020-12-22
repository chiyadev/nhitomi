import React, { Dispatch, memo } from "react";
import { chakra, Checkbox, Heading, HStack, Icon, Link, VStack } from "@chakra-ui/react";
import { useQuery } from "../../../utils/query";
import { useT } from "../../../locales";
import { FaFilter } from "react-icons/fa";
import { ScraperIcons } from "../../../utils/constants";
import { useClientInfo } from "../../../utils/client";
import NextLink from "next/link";
import { useRouter } from "next/router";
import { ScraperType } from "nhitomi-api";

function toggleSource(str: string, value: ScraperType) {
  const array = str.split(",");

  if (array.includes(value)) {
    return array.filter((v) => v !== value).join(",");
  } else {
    return array.concat(value).join(",");
  }
}

const SourceCheck = ({ setOpen }: { setOpen: Dispatch<boolean> }) => {
  const t = useT();
  const [current] = useQuery("source");
  const info = useClientInfo();

  const { query } = useRouter();

  return (
    <VStack align="start" spacing={4}>
      <Heading size="sm">
        <HStack spacing={2}>
          <Icon as={FaFilter} />
          <div>{t("BookListing.QueryDrawer.SourceCheck.title")}</div>
        </HStack>
      </Heading>

      <VStack align="start" spacing={2}>
        {info?.scrapers.map(({ type, name }) => (
          <NextLink key={type} href={{ query: { ...query, source: toggleSource(current, type) } }} passHref>
            <Link onClick={() => setOpen(false)}>
              <HStack>
                <Checkbox isChecked={current.includes(type)} />
                <chakra.img w={6} src={ScraperIcons[type]} borderRadius="md" />
                <div>{name}</div>
              </HStack>
            </Link>
          </NextLink>
        ))}
      </VStack>
    </VStack>
  );
};

export default memo(SourceCheck);
