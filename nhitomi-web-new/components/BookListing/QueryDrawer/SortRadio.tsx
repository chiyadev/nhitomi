import React, { Dispatch, memo } from "react";
import { Heading, HStack, Icon, Radio, RadioGroup, VStack } from "@chakra-ui/react";
import { useQuery } from "../../../utils/query";
import { BookSort } from "nhitomi-api";
import { useT } from "../../../locales";
import { FaSortAmountUp } from "react-icons/fa";

const SortRadio = ({ setOpen }: { setOpen: Dispatch<boolean> }) => {
  const t = useT();
  const [sort, setSort] = useQuery("sort");

  return (
    <VStack align="stretch" spacing={4}>
      <Heading size="sm">
        <HStack spacing={2}>
          <Icon as={FaSortAmountUp} />
          <div>{t("BookListing.QueryDrawer.SortRadio.title")}</div>
        </HStack>
      </Heading>

      <RadioGroup
        name="sort"
        value={sort}
        onChange={async (value) => {
          setOpen(false);

          await setSort(value as BookSort, "push");
        }}
      >
        <VStack align="start" spacing={2}>
          {Object.values(BookSort).map((sort) => (
            <Radio key={sort} value={sort} cursor="pointer">
              {t("BookSort", { value: sort })}
            </Radio>
          ))}
        </VStack>
      </RadioGroup>
    </VStack>
  );
};

export default memo(SortRadio);
