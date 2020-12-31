import React, { memo, ReactNode, useEffect, useState } from "react";
import { HStack, Icon, Link, Spinner, VStack } from "@chakra-ui/react";
import DateDisplay from "../../DateDisplay";
import { Book, BookContent } from "nhitomi-api";
import { useT } from "../../../locales";
import { FaBookOpen, FaHistory, FaTimes, FaUpload } from "react-icons/fa";
import { createApiClient } from "../../../utils/client";
import { useErrorToast } from "../../../utils/hooks";

const InfoText = ({ book, content }: { book: Book; content: BookContent }) => {
  const t = useT();

  return (
    <VStack align="start" color="gray.200" spacing={0}>
      <InfoLine icon={FaBookOpen}>{t("BookReader.Info.InfoText.pageCount", { count: content.pageCount })}</InfoLine>
      <InfoLine icon={FaUpload}>
        {t("BookReader.Info.InfoText.createdTime", { time: <DateDisplay date={book.createdTime} /> })}
      </InfoLine>
      <InfoLine icon={FaHistory}>
        {t("BookReader.Info.InfoText.updatedTime", { time: <DateDisplay date={book.updatedTime} /> })}
      </InfoLine>

      <RefreshTimeText book={book} content={content} />
    </VStack>
  );
};

const InfoLine = ({ icon, children }: { icon: any; children?: ReactNode }) => {
  return (
    <HStack spacing={1}>
      <Icon as={icon} />
      <div>{children}</div>
    </HStack>
  );
};

const RefreshTimeText = ({ book, content }: { book: Book; content: BookContent }) => {
  const t = useT();
  const error = useErrorToast();
  const [state, setState] = useState<boolean | Error>(false);

  useEffect(() => {
    (async () => {
      try {
        const client = createApiClient();

        await client.book.refreshBook({
          id: book.id,
          refreshContentRequest: {
            contentId: content.id,
          },
        });

        setState(true);
      } catch (e) {
        setState(e);
      }
    })();
  }, []);

  if (state instanceof Error) {
    return (
      <Link as="button" color="red.300" onClick={() => error(state)}>
        <InfoLine icon={FaTimes}>{t("BookReader.Info.InfoText.refreshFail")}</InfoLine>
      </Link>
    );
  } else if (state) {
    return null;
  } else {
    return <InfoLine icon={Spinner}>{t("BookReader.Info.InfoText.refreshing")}</InfoLine>;
  }
};

export default memo(InfoText);
