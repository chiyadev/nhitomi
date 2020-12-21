import React, { Fragment, memo } from "react";
import { Book, BookContent, ScraperType } from "nhitomi-api";
import {
  Avatar,
  HStack,
  Icon,
  Menu,
  MenuButton,
  MenuDivider,
  MenuItemOption,
  MenuList,
  MenuOptionGroup,
  Tag,
  TagLabel,
  Text,
  Wrap,
  WrapItem,
} from "@chakra-ui/react";
import { LanguageTypes, ScraperIcons, ScraperTypes } from "../../../utils/constants";
import Router from "next/router";
import { FaExternalLinkAlt } from "react-icons/fa";
import { useT } from "../../../locales";

const SourceList = ({ book, selectedContent }: { book: Book; selectedContent: BookContent }) => {
  return (
    <Wrap spacing={1}>
      {ScraperTypes.map((source) => {
        const contents = book.contents
          .filter((content) => content.source === source)
          .sort((a, b) => b.id.localeCompare(a.id));

        if (!contents.length) {
          return null;
        }

        return <Item key={source} book={book} source={source} contents={contents} selectedContent={selectedContent} />;
      })}
    </Wrap>
  );
};

const Item = ({
  book,
  source,
  contents,
  selectedContent,
}: {
  book: Book;
  source: ScraperType;
  contents: BookContent[];
  selectedContent: BookContent;
}) => {
  const t = useT();

  return (
    <WrapItem>
      <Menu autoSelect={false} preventOverflow>
        <MenuButton as={Tag} size="lg" cursor="pointer">
          <Avatar src={ScraperIcons[source]} size="xs" ml={-1} mr={2} verticalAlign="middle" />
          <TagLabel verticalAlign="middle">{source}</TagLabel>
        </MenuButton>

        <MenuList>
          {LanguageTypes.map((language) => {
            const languageContents = contents.filter((content) => content.language === language);

            return {
              language,
              child: !languageContents.length ? null : (
                <MenuOptionGroup
                  type="radio"
                  title={t("LanguageType", { value: language.replace("-", "") })}
                  value={selectedContent.id}
                >
                  {languageContents.map((content) => {
                    const url = new URL(content.sourceUrl);
                    const readerUrl = `/books/${book.id}/contents/${content.id}`;

                    return (
                      <MenuItemOption
                        key={content.id}
                        as="a"
                        href={readerUrl}
                        value={content.id}
                        onClick={async (e) => {
                          if (!e.ctrlKey && !e.shiftKey && !e.altKey) {
                            e.preventDefault();

                            await Router.push({
                              pathname: readerUrl,
                            });
                          }
                        }}
                      >
                        <HStack maxW="50vw">
                          <Text isTruncated flex={1}>
                            {decodeURI(url.hostname + url.pathname + url.search)}
                          </Text>

                          <a
                            href={url.href}
                            target="_blank"
                            rel="noopener noreferrer"
                            onClick={(e) => e.stopPropagation()}
                          >
                            <Icon as={FaExternalLinkAlt} fontSize="sm" color="blue.300" />
                          </a>
                        </HStack>
                      </MenuItemOption>
                    );
                  })}
                </MenuOptionGroup>
              ),
            };
          })
            .filter((x) => x.child)
            .map(({ language, child }, i, a) => (
              <Fragment key={language}>
                {child}
                {i !== a.length - 1 && <MenuDivider />}
              </Fragment>
            ))}
        </MenuList>
      </Menu>
    </WrapItem>
  );
};

export default memo(SourceList);
