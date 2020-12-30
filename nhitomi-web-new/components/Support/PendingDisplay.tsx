import React, { memo } from "react";
import { useT } from "../../locales";
import Layout from "../Layout";
import LayoutBody from "../LayoutBody";
import Header from "../Header";
import HeaderTitle from "./HeaderTitle";
import { Button, ButtonGroup, Heading, HStack, Icon, VStack } from "@chakra-ui/react";
import Banner from "./Banner";
import { FaChevronLeft, FaHeart } from "react-icons/fa";
import NextLink from "next/link";

const PendingDisplay = () => {
  const t = useT();

  return (
    <Layout title={[t("Support.PendingDisplay.title"), t("Support.title")]}>
      <Header back title={<HeaderTitle />} />

      <LayoutBody>
        <VStack align="stretch" spacing={8}>
          <Banner />

          <HStack align="baseline" spacing={2}>
            <Heading size="lg">Thank you!</Heading>
            <Icon as={FaHeart} fontSize="xl" color="pink.300" transform="rotate(20deg)" />
          </HStack>

          <VStack align="start" spacing={2}>
            <div>Your payment was successful.</div>

            <div>
              It may take a while for your supporter status to be updated. This process usually takes up to a few
              minutes.
            </div>
          </VStack>

          <ButtonGroup>
            <NextLink href="/support" passHref>
              <Button as="a" colorScheme="blue" leftIcon={<Icon as={FaChevronLeft} />}>
                {t("Support.PendingDisplay.back")}
              </Button>
            </NextLink>
          </ButtonGroup>
        </VStack>
      </LayoutBody>
    </Layout>
  );
};

export default memo(PendingDisplay);
