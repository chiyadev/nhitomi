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
        colors: {
          discord: {
            100: "#e3e7f8",
            200: "#c7d0f0",
            300: "#aab8e9",
            400: "#8ea1e1",
            500: "#7289da",
            600: "#5b6eae",
            700: "#445283",
            800: "#2e3757",
            900: "#171b2c",
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
