import React, { Dispatch, memo, RefObject, useCallback, useEffect, useMemo, useRef, useState } from "react";
import { DownloadTarget } from ".";
import {
  Button,
  ButtonGroup,
  Divider,
  HStack,
  Icon,
  ModalBody,
  ModalFooter,
  ModalHeader,
  VStack,
} from "@chakra-ui/react";
import { FaDownload, FaTimes } from "react-icons/fa";
import ItemDisplay from "./ItemDisplay";
import { useT } from "../../locales";

const Content = ({
  focusRef,
  targets,
  setOpen,
  autoClose,
}: {
  focusRef: RefObject<HTMLButtonElement>;
  targets: DownloadTarget[];
  setOpen: Dispatch<boolean>;
  autoClose: boolean;
}) => {
  const t = useT();
  const sessions = useRef(0);

  const [completed, setCompleted] = useState(0);
  const onComplete = useCallback(() => setCompleted((i) => i + 1), []);

  useEffect(() => {
    autoClose && completed >= targets.length && setOpen(false);
  }, [completed, targets, autoClose]);

  return (
    <>
      <ModalHeader>
        <HStack spacing={2}>
          <Icon as={FaDownload} />
          <div>{t("BookDownloader.title")}</div>

          {targets.length > 1 && (
            <div>
              ({completed}/{targets.length})
            </div>
          )}
        </HStack>
      </ModalHeader>

      <ModalBody>
        <VStack align="stretch" spacing={4} divider={<Divider />}>
          {useMemo(
            () =>
              targets.map(({ book, content }, i) => {
                return (
                  <ItemDisplay
                    key={`${book.id}/${content.id}`}
                    book={book}
                    content={content}
                    index={i}
                    sessions={sessions}
                    onComplete={onComplete}
                  />
                );
              }),
            [targets, onComplete]
          )}
        </VStack>
      </ModalBody>

      <ModalFooter>
        <ButtonGroup>
          <Button
            ref={focusRef}
            leftIcon={completed ? undefined : <Icon as={FaTimes} />}
            onClick={() => setOpen(false)}
          >
            {t(completed ? "BookDownloader.close" : "BookDownloader.cancel")}
          </Button>
        </ButtonGroup>
      </ModalFooter>
    </>
  );
};

export default memo(Content);
