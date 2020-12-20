import "@openfonts/lexend-deca_all";
import "@openfonts/m-plus-rounded-1c_japanese";
import { ChakraProvider, extendTheme } from "@chakra-ui/react";
import React, { memo } from "react";
import { AppProps } from "next/app";
import { useScrollPreserver } from "../utils/scrollPreserver";
import NProgress from "../components/NProgress";
import "../components/NProgress.css";
import { loadPolyfills } from "../utils/polyfills";

loadPolyfills();

const fallbackFonts =
  '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif, "Apple Color Emoji", "Segoe UI Emoji", "Segoe UI Symbol"';

const App = ({ Component, pageProps }: AppProps) => {
  useScrollPreserver();

  return (
    <ChakraProvider
      theme={extendTheme({
        config: {
          initialColorMode: "dark",
        },
        fonts: {
          body: `'Lexend Deca', "M PLUS 1p", ${fallbackFonts}`,
          heading: `'Lexend Deca', "M PLUS 1p", ${fallbackFonts}`,
        },
        fontSizes: {
          xs: "10px",
          sm: "12px",
          md: "14px",
          lg: "16px",
          xl: "18px",
        },
        styles: {
          global: {
            body: {
              bg: "gray.900",
              fontSize: "md",
            },
          },
        },
      })}
    >
      <NProgress />
      <Component {...pageProps} />
    </ChakraProvider>
  );
};

export default memo(App);
