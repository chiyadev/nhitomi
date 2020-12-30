import React, { memo } from "react";
import Layout from "../Layout";
import DiscordButton from "./OAuth/DiscordButton";
import Background from "./Background";
import { Center, chakra, Heading, Icon, VStack } from "@chakra-ui/react";
import Logo192x192 from "../../assets/logo-192x192.png";
import TwitterButton from "./OAuth/TwitterButton";
import { FaHeart } from "react-icons/fa";

const Auth = () => {
  return (
    <Layout center>
      <Background />

      <VStack
        position="relative"
        align="stretch"
        spacing={4}
        w="full"
        maxW="sm"
        px={4}
        py={6}
        m={4}
        bg="gray.900"
        borderRadius="md"
        boxShadow="md"
        textAlign="center"
      >
        <Center mt={-32}>
          <chakra.img src={Logo192x192} w={40} boxShadow="md" />
        </Center>

        <VStack spacing={2}>
          <Heading size="md">nhitomi</Heading>
          <chakra.div fontSize="sm">Open-source doujinshi aggregator</chakra.div>
        </VStack>

        <VStack spacing={1}>
          <DiscordButton />
          <TwitterButton />
        </VStack>

        <Icon
          as={FaHeart}
          position="absolute"
          color="pink.300"
          top={-6}
          right={3}
          fontSize="3xl"
          transform="rotate(20deg)"
        />
      </VStack>
    </Layout>
  );
};

export default memo(Auth);
