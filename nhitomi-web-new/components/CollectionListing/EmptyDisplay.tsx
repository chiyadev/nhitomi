import React, { memo, useState } from "react";
import { chakra, Icon, Link, VStack } from "@chakra-ui/react";
import { FaFolder } from "react-icons/fa";
import { useT } from "../../locales";
import CollectionCreator from "../CollectionCreator";
import { useRouter } from "next/router";

const EmptyDisplay = () => {
  const t = useT();
  const router = useRouter();
  const [create, setCreate] = useState(false);

  return (
    <>
      <CollectionCreator
        open={create}
        setOpen={setCreate}
        onCreate={async (collection) => {
          setCreate(false);

          await router.push(`/collections/${collection.id}`);
        }}
      />

      <VStack spacing={2}>
        <Icon as={FaFolder} fontSize="xl" />

        <chakra.div fontSize="sm">{t("CollectionListing.EmptyDisplay.text")}</chakra.div>

        <Link as="button" color="blue.500" fontSize="sm" onClick={() => setCreate(true)}>
          {t("CollectionListing.EmptyDisplay.create")}
        </Link>
      </VStack>
    </>
  );
};

export default memo(EmptyDisplay);
