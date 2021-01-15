import React, { memo } from "react";
import { GetStripeInfoResponse } from "nhitomi-api";
import { chakra, Heading, ListItem, Progress, UnorderedList, VStack } from "@chakra-ui/react";

const ProgressSection = ({ stripe: { donationProgress, donationGoal } }: { stripe: GetStripeInfoResponse }) => {
  if (!donationGoal) {
    return null;
  }

  return (
    <VStack align="stretch" spacing={8}>
      <Heading size="lg">Monthly progress ({Math.round((donationProgress / donationGoal) * 100)}%)</Heading>

      <VStack align="stretch" spacing={2}>
        <Progress
          value={donationProgress}
          min={0}
          max={donationGoal || donationProgress}
          colorScheme="pink"
          borderRadius="md"
          boxShadow="md"
          hasStripe
          isAnimated
          size="lg"
        />

        <chakra.div textAlign="center" fontSize="sm">
          ${Math.round(donationProgress)} / ${Math.round(donationGoal)}
        </chakra.div>
      </VStack>

      <VStack align="start" spacing={2}>
        <div>
          nhitomi requires around ${Math.round(donationGoal)} USD per month to exist. Where does all this money go?
        </div>

        <UnorderedList pl={4}>
          <ListItem>Database servers - $10</ListItem>
          <ListItem>Website and API servers - $10</ListItem>
          <ListItem>Discord bot server - $5</ListItem>
          <ListItem>Image storage nodes - $20</ListItem>
          <ListItem>Proxy servers, scraper IPs and bandwidth costs - $30</ListItem>
        </UnorderedList>

        <div>
          While these things may seem like small monthly expenses, they really start accumulating when the goal is not
          reached.
        </div>
      </VStack>
    </VStack>
  );
};

export default memo(ProgressSection);
