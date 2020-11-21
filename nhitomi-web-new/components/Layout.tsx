import React, { memo, ReactNode } from "react";
import Head from "next/head";
import Footer from "./Footer";
import { Box, Flex, Spacer } from "@chakra-ui/layout";

const Layout = ({ children, title = "nhitomi" }: { children?: ReactNode; title?: string }) => (
  <>
    <Head>
      <meta charSet="utf-8" />

      <meta name="viewport" content="initial-scale=1.0, width=device-width" />
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

      <title>{title}</title>
    </Head>

    <Flex direction="column" w="full" h="100vh" maxW="1200px" mx="auto">
      <Box>{children}</Box>
      <Spacer />
      <Footer />
    </Flex>
  </>
);

export default memo(Layout);
