import React, { memo, ReactNode } from "react";
import Head from "next/head";
import Footer from "./Footer";
import { Center, chakra, Fade, Flex } from "@chakra-ui/react";
import MaintenanceAlert from "./MaintenanceAlert";

const Layout = ({
  children,
  title = [],
  center = false,
}: {
  children?: ReactNode;
  title?: (string | undefined)[];
  center?: boolean;
}) => (
  <Fade in>
    <Head>
      <meta charSet="utf-8" />

      <meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1" />
      <meta name="application-name" content="nhitomi" />
      <meta name="apple-mobile-web-app-title" content="nhitomi" />
      <meta name="msapplication-TileColor" content="#74c0fc" />
      <meta name="theme-color" content="#74c0fc" />

      <link rel="manifest" href="/site.webmanifest" />
      <link rel="apple-touch-icon" sizes="180x180" href="/apple-touch-icon.png" />
      <link rel="icon" href="/favicon.ico" />
      <link rel="icon" type="image/png" sizes="32x32" href="/favicon-32x32.png" />
      <link rel="icon" type="image/png" sizes="16x16" href="/favicon-16x16.png" />
      <link rel="mask-icon" href="/safari-pinned-tab.svg" color="#74c0fc" />

      <title>{[...title.map((x) => x?.trim()).filter((x) => x), "nhitomi"].join(" · ")}</title>
    </Head>

    <Flex direction="column" minH="100vh">
      <MaintenanceAlert />

      {center ? <Center flex={1}>{children}</Center> : <chakra.div flex={1}>{children}</chakra.div>}

      <Footer />
    </Flex>
  </Fade>
);

export default memo(Layout);
