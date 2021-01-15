import React, { memo } from "react";
import { Heading, HStack, Icon, Link, VStack } from "@chakra-ui/react";
import { FaHeart } from "react-icons/fa";
import QuoteWrap from "./QuoteWrap";

const PleadSection = () => {
  return (
    <VStack align="stretch" spacing={8}>
      <HStack align="baseline" spacing={2}>
        <Heading size="lg">Support nhitomi!</Heading>
        <Icon as={FaHeart} fontSize="xl" color="pink.300" transform="rotate(20deg)" />
      </HStack>

      <QuoteWrap>
        <div>
          nhitomi is a completely free service, but serving thousands of visitors everyday, maintaining our
          infrastructure and developing the project is quite costly in both time and money.
        </div>

        <div>
          We are an{" "}
          <Link href="https://github.com/chiyadev/nhitomi" color="blue.300" isExternal>
            open-source project
          </Link>{" "}
          that does not rely on annoying advertisements or pop-ups to pay the bills. If profit was our motive, we would
          have made a crappy PHP website and slap a few ads in every corner. But that's not what we did.
        </div>

        <div>
          Small donations are what keeps nhitomi alive. If you are financially able, please help us by becoming a
          supporter!
        </div>
      </QuoteWrap>
    </VStack>
  );
};

export default memo(PleadSection);
