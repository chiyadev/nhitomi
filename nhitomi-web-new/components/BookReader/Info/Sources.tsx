import React, { memo } from "react";
import { Book, BookContent, ScraperType } from "nhitomi-api";
import {
  Avatar,
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
import { LanguageTypes, ScraperTypes } from "../../../utils/constants";
import { SourceIcons } from "../../../utils/icons";
import Router from "next/router";

const Sources = ({ book, selectedContent }: { book: Book; selectedContent: BookContent }) => {
  return (
    <Wrap spacing={1}>
      {ScraperTypes.map((source) => {
        const contents = book.contents
          .filter((content) => content.source === source)
          .sort((a, b) => b.id.localeCompare(a.id));

        if (!contents.length) return null;

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
  return (
    <WrapItem>
      <Menu autoSelect={false} preventOverflow>
        <MenuButton as={Tag} size="lg" cursor="pointer" lineHeight={undefined}>
          <Avatar src={SourceIcons[source]} size="xs" ml={-1} mr={2} />
          <TagLabel fontSize="sm">{source}</TagLabel>
        </MenuButton>

        <MenuList>
          {LanguageTypes.map((language) => {
            const languageContents = contents.filter((content) => content.language === language);
            if (!languageContents.length) return null;

            return (
              <MenuOptionGroup key={language} type="radio" title={language} value={selectedContent.id}>
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
                      <Text isTruncated maxW="50vw">
                        {decodeURI(url.hostname + url.pathname + url.search)}
                      </Text>
                    </MenuItemOption>
                  );
                })}
              </MenuOptionGroup>
            );
          })
            .filter((x) => x)
            .map((x, i, a) => (
              <>
                {x}
                {i !== a.length - 1 && <MenuDivider />}
              </>
            ))}
        </MenuList>
      </Menu>
    </WrapItem>
  );
};

export default memo(Sources);
