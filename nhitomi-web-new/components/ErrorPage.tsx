import React, { memo, ReactNode } from "react";
import Layout from "./Layout";
import Megumi404 from "../assets/Statuses/Megumi404.jpg";
import Chino500 from "../assets/Statuses/Chino500.png";
import { Button, chakra, Code, Icon, VStack } from "@chakra-ui/react";
import { useConfig } from "../utils/config";
import { FaChevronLeft, FaSignOutAlt } from "react-icons/fa";
import { useRouter } from "next/router";
import NextLink from "next/link";

const ErrorPage = ({ message }: { message: string }) => {
  let content: ReactNode;

  if (message.match(/not found/i)) {
    content = <NotFoundPage message={message} />;
  } else if (message.match(/unauthorized/i)) {
    content = <UnauthorizedPage message={message} />;
  } else {
    content = <OtherErrorPage message={message} />;
  }

  return (
    <Layout center title={[message]}>
      <chakra.div m={4} textAlign="center">
        {content}
      </chakra.div>
    </Layout>
  );
};

const StatusImage = ({ src }: { src: string }) => {
  return <chakra.img src={src} borderRadius="md" boxShadow="md" maxH="xs" />;
};

const StatusMessage = ({ children }: { children?: ReactNode }) => {
  return <Code fontSize="lg">{children}</Code>;
};

const NotFoundPage = ({ message }: { message: string }) => {
  return (
    <VStack spacing={6}>
      <StatusImage src={Megumi404} />
      <StatusMessage>{message}</StatusMessage>

      <NextLink href="/" passHref>
        <Button as="a" colorScheme="blue" leftIcon={<Icon as={FaChevronLeft} />}>
          Back
        </Button>
      </NextLink>
    </VStack>
  );
};

const UnauthorizedPage = ({ message }: { message: string }) => {
  const router = useRouter();
  const [, setToken] = useConfig("token");

  return (
    <VStack spacing={6}>
      <StatusImage src={Chino500} />
      <StatusMessage>{message}</StatusMessage>

      <Button
        colorScheme="blue"
        leftIcon={<Icon as={FaSignOutAlt} />}
        onClick={() => {
          setToken(undefined);
          setTimeout(() => router.reload(), 200);
        }}
      >
        Sign out
      </Button>
    </VStack>
  );
};

const OtherErrorPage = ({ message }: { message: string }) => {
  return (
    <VStack spacing={6}>
      <StatusImage src={Chino500} />
      <StatusMessage>{message}</StatusMessage>
    </VStack>
  );
};

export default memo(ErrorPage);
