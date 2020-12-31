import React, { memo, ReactNode } from "react";
import { chakra, Grid, GridItem, Heading, HStack, Icon, VStack } from "@chakra-ui/react";
import styles from "./BenefitsSection.module.css";
import { NonSupporterPageLimit } from "../../utils/constants";
import { FaBolt, FaFolderOpen, FaStar } from "react-icons/fa";
import BenefitPageLimit from "../../assets/Support/BenefitPageLimit.jpg";
import BenefitFastDownload from "../../assets/Support/BenefitFastDownload.jpg";
import BenefitCollectionDownload from "../../assets/Support/BenefitCollectionDownload.jpg";

const BenefitsSection = () => {
  return (
    <VStack align="stretch" spacing={8}>
      <HStack spacing={2}>
        <Heading size="lg">What do I get?</Heading>
      </HStack>

      <Grid className={styles.grid} gap={4}>
        <GridItem>
          <BenefitItem heading="Unlimited reading power" icon={FaStar} image={BenefitPageLimit}>
            The {NonSupporterPageLimit} page restriction is lifted, so you can read any number of pages.
          </BenefitItem>
        </GridItem>

        <GridItem>
          <BenefitItem heading="Supercharged downloads" icon={FaBolt} image={BenefitFastDownload}>
            Download multiple books without bandwidth limit.
          </BenefitItem>
        </GridItem>

        <GridItem>
          <BenefitItem heading="Collection downloads" icon={FaFolderOpen} image={BenefitCollectionDownload}>
            Batch download entire collections with a single click.
          </BenefitItem>
        </GridItem>
      </Grid>
    </VStack>
  );
};

const BenefitItem = ({
  heading,
  children,
  icon,
  image,
}: {
  heading?: ReactNode;
  children?: ReactNode;
  icon?: any;
  image?: string;
}) => {
  return (
    <HStack align="baseline" spacing={2}>
      {icon && <Icon as={icon} />}

      <VStack flex={1} align="stretch" spacing={4}>
        <div>
          <chakra.div fontSize="lg">{heading}</chakra.div>
          <chakra.div fontSize="sm" color="gray.500">
            {children}
          </chakra.div>
        </div>

        {image && <chakra.img src={image} h={52} objectFit="cover" boxShadow="md" borderRadius="md" />}
      </VStack>
    </HStack>
  );
};

export default memo(BenefitsSection);
