import "inter-ui/inter.css";
import "@openfonts/m-plus-1p_japanese/index.css";
import { ChakraProvider, extendTheme } from "@chakra-ui/react";
import React, { memo } from "react";
import { AppProps } from "next/app";
import { useScrollPreserver } from "../utils/scrollPreserver";

const App = ({ Component, pageProps }: AppProps) => {
  useScrollPreserver();

  return (
    <ChakraProvider
      theme={extendTheme({
        config: {
          initialColorMode: "dark",
        },
        styles: {
          global: {
            body: {
              bg: "gray.900",
              fontFamily:
                'Inter, "M PLUS 1p", -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif, "Apple Color Emoji", "Segoe UI Emoji", "Segoe UI Symbol"',
            },
          },
        },
      })}
    >
      <Component {...pageProps} />
    </ChakraProvider>
  );
};

export default memo(App);
