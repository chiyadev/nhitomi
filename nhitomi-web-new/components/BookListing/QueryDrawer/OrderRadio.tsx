import React, { Dispatch, memo } from "react";
import { Heading, HStack, Icon, Radio, RadioGroup, VStack } from "@chakra-ui/react";
import { useQuery } from "../../../utils/query";
import { SortDirection } from "nhitomi-api";
import { useT } from "../../../locales";
import { FaSortAlphaUp } from "react-icons/fa";

const OrderRadio = ({ setOpen }: { setOpen: Dispatch<boolean> }) => {
  const t = useT();
  const [order, setOrder] = useQuery("order");

  return (
    <VStack align="stretch" spacing={4}>
      <Heading size="sm">
        <HStack spacing={2}>
          <Icon as={FaSortAlphaUp} />
          <div>{t("BookListing.QueryDrawer.OrderRadio.title")}</div>
        </HStack>
      </Heading>

      <RadioGroup
        name="order"
        value={order}
        onChange={async (value) => {
          setOpen(false);

          await setOrder(value as SortDirection, "push");
        }}
      >
        <VStack align="start" spacing={2}>
          {Object.values(SortDirection).map((order) => (
            <Radio key={order} value={order} cursor="pointer">
              {t("SortDirection", { value: order })}
            </Radio>
          ))}
        </VStack>
      </RadioGroup>
    </VStack>
  );
};

export default memo(OrderRadio);
