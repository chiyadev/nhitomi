import { chakra, Heading, HStack, Icon, VStack } from "@chakra-ui/react";
import React, { memo } from "react";
import { useClientInfoAuth } from "../../utils/client";
import QuoteWrap from "./QuoteWrap";
import { FaHeart } from "react-icons/fa";

const SupporterSection = () => {
  const info = useClientInfoAuth();
  const supportInfo = info?.user.supporterInfo;

  const totalMonths = Math.round(((supportInfo?.totalDays || 0) / 365.2422) * 12);
  const spending = supportInfo?.totalSpending || 0;
  const endDays = Math.ceil(((supportInfo?.endTime?.getTime() || 0) - Date.now()) / 86400000);

  return (
    <VStack align="stretch" spacing={8}>
      <HStack align="baseline" spacing={2}>
        <Heading size="lg">You are a supporter!</Heading>
        <Icon as={FaHeart} fontSize="xl" color="pink.300" transform="rotate(20deg)" />
      </HStack>

      <QuoteWrap>
        <div>
          You are supporting nhitomi for{" "}
          <chakra.span color="pink.200">
            {totalMonths} {totalMonths === 1 ? "month" : "months"}
          </chakra.span>{" "}
          and have contributed <chakra.span color="pink.200">${spending.toFixed(2)}</chakra.span> in total. Your support
          ends in{" "}
          <chakra.span color="pink.200">
            {endDays} {endDays === 1 ? "day" : "days"}
          </chakra.span>
          .
        </div>

        <div>
          It is thanks to generous people like you that nhitomi can afford its server infrastructure, allocate more
          development time for new features, and deliver a free service without annoying advertisements and pop-ups.
        </div>

        <div>We can't thank you enough! You're absolutely amazing.</div>
      </QuoteWrap>
    </VStack>
  );
};

export default memo(SupporterSection);
