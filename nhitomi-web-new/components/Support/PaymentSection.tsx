import React, { Dispatch, memo, useState } from "react";
import {
  Button,
  Center,
  chakra,
  Heading,
  HStack,
  Link,
  ListItem,
  Slider,
  SliderFilledTrack,
  SliderThumb,
  SliderTrack,
  UnorderedList,
  VStack,
} from "@chakra-ui/react";
import MegumiHappy from "../../assets/Support/MegumiHappy.jpg";
import { createApiClient, useClientInfoAuth } from "../../utils/client";
import { GetStripeInfoResponse } from "nhitomi-api";
import { useErrorToast, useWindowValue } from "../../utils/hooks";
import { loadStripe } from "@stripe/stripe-js";

const PaymentSection = ({ stripe }: { stripe: GetStripeInfoResponse }) => {
  const info = useClientInfoAuth();
  const [duration, setDuration] = useState(3);

  const isMirror = useWindowValue(false, () => !info || new URL(info.publicUrl).host !== window.location.host);

  return (
    <VStack align="stretch" spacing={8}>
      <HStack spacing={2}>
        <Heading size="lg">Let's get it!</Heading>
      </HStack>

      <VStack align="start" spacing={2}>
        <div>Please note:</div>

        <UnorderedList pl={4}>
          <ListItem>nhitomi supporter is a non-recurring payment.</ListItem>
          <ListItem>
            Supporter price is ${stripe.supporterPrice.toFixed(2)} USD per month.{" "}
            <chakra.small whiteSpace="pre" color="gray.500">
              (365.2422 ÷ 12 days)
            </chakra.small>
          </ListItem>
          <ListItem>If you are already a supporter, the duration will be extended.</ListItem>
        </UnorderedList>
      </VStack>

      {isMirror && (
        <div>
          Payment is not supported on mirror domains! Please visit the{" "}
          <Link href={info?.publicUrl} color="blue.300">
            main website
          </Link>
          .
        </div>
      )}

      <Center>
        <VStack align="stretch" spacing={4} w="full" maxW="xs">
          <PaymentButton stripe={stripe} duration={duration} disabled={isMirror} />
          <DurationAdjust stripe={stripe} duration={duration} setDuration={setDuration} />
        </VStack>
      </Center>
    </VStack>
  );
};

const PaymentButton = ({
  stripe: { apiKey, supporterPrice },
  duration,
  disabled,
}: {
  stripe: GetStripeInfoResponse;
  duration: number;
  disabled?: boolean;
}) => {
  const info = useClientInfoAuth();
  const error = useErrorToast();
  const [load, setLoad] = useState(false);

  return (
    <chakra.div position="relative" h={32}>
      <chakra.img
        src={MegumiHappy}
        position="absolute"
        top={0}
        left={0}
        w="full"
        h="full"
        zIndex={-1}
        objectFit="cover"
        opacity={0.5}
        borderRadius="md"
      />

      <Button
        variant="ghost"
        w="full"
        h="full"
        fontWeight="normal"
        isLoading={load}
        disabled={disabled}
        borderColor="pink.300"
        borderWidth={1}
        onClick={async () => {
          setLoad(true);

          try {
            const client = createApiClient();
            const stripe = await loadStripe(apiKey);

            if (!info || !client || !stripe) {
              error(Error("Could not load Stripe."));
              return;
            }

            const { sessionId } = await client.user.createUserSupporterCheckout({
              id: info.user.id,
              createSupporterCheckoutRequest: {
                amount: duration * supporterPrice,
              },
            });

            await stripe.redirectToCheckout({ sessionId });
          } catch (e) {
            console.error(e);
            error(e);
          } finally {
            setLoad(false);
          }
        }}
      >
        <Center>
          <VStack spacing={2}>
            <Heading size="md">Proceed to payment</Heading>
            <chakra.div>
              Become a supporter for {duration} {duration === 1 ? "month" : "months"}
            </chakra.div>
          </VStack>
        </Center>
      </Button>
    </chakra.div>
  );
};

const DurationAdjust = ({
  stripe: { supporterPrice },
  duration,
  setDuration,
  disabled,
}: {
  stripe: GetStripeInfoResponse;
  duration: number;
  setDuration: Dispatch<number>;
  disabled?: boolean;
}) => {
  return (
    <VStack spacing={2}>
      <VStack spacing={0}>
        <div>{duration * supporterPrice} USD</div>

        <chakra.div fontSize="sm" color="gray.500">
          ($3/month)
        </chakra.div>
      </VStack>

      <Slider w="full" colorScheme="pink" min={1} max={12} value={duration} onChange={setDuration} disabled={disabled}>
        <SliderTrack>
          <SliderFilledTrack />
        </SliderTrack>
        <SliderThumb />
      </Slider>
    </VStack>
  );
};

export default memo(PaymentSection);
