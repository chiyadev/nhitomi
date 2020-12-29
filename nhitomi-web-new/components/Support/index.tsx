import React, { memo } from "react";
import Layout from "../Layout";
import { useT } from "../../locales";
import Header from "../Header";
import HeaderTitle from "./HeaderTitle";
import { Divider, VStack } from "@chakra-ui/react";
import LayoutBody from "../LayoutBody";
import PleadSection from "./PleadSection";
import BenefitsSection from "./BenefitsSection";
import PaymentSection from "./PaymentSection";
import { GetStripeInfoResponse } from "nhitomi-api";
import Banner from "./Banner";
import { useClientInfoAuth } from "../../utils/client";
import SupporterSection from "./SupporterSection";

const Support = ({ stripe }: { stripe: GetStripeInfoResponse }) => {
  const t = useT();
  const info = useClientInfoAuth();

  return (
    <Layout title={[t("Support.title")]}>
      <Header title={<HeaderTitle />} />

      <LayoutBody px={4}>
        <VStack align="stretch" spacing={8}>
          <Banner />

          <VStack align="stretch" spacing={8} divider={<Divider />}>
            {info?.user.isSupporter ? <SupporterSection /> : <PleadSection />}

            <BenefitsSection />
            <PaymentSection stripe={stripe} />
          </VStack>
        </VStack>
      </LayoutBody>
    </Layout>
  );
};

export default memo(Support);
