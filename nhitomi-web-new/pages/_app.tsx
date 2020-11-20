import { ChakraProvider, extendTheme } from "@chakra-ui/react";
import React from "react";
import { AppProps } from "next/app";

const App = ({ Component, pageProps }: AppProps) => {
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
            },
          },
        },
      })}
    >
      <Component {...pageProps} />
    </ChakraProvider>
  );
};

export default App;
