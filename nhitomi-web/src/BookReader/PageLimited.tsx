import React, { ReactNode, useState } from "react";
import { Book, BookContent, ScraperType } from "nhitomi-api";
import { useClientInfo } from "../ClientManager";
import { ScraperTypes } from "../orderedConstants";
import { useContentSelector } from "../Components/BookList";
import { ArrowRightOutlined, HeartFilled, ReadOutlined } from "@ant-design/icons";
import { Anchor } from "../Components/Anchor";
import { animated, useSpring } from "react-spring";
import { getColor } from "../theme";
import { SupportLink } from "../Support";
import { css, cx } from "emotion";
import { FormattedMessage } from "react-intl";
import { SourceIcon } from "../Components/SourceIcon";

export const PageLimited = ({ book, content }: { book: Book; content: BookContent }) => {
  const selectContent = useContentSelector();

  const style = useSpring({
    from: { opacity: 0 },
    to: { opacity: 1 },
  });

  return (
    <animated.div style={style} className="absolute transform-center w-full max-w-xl p-4">
      <div className="bg-white text-black rounded-lg overflow-hidden divide-y divide-gray-lighter shadow-lg">
        <div className="p-4 text-sm text-gray-darker">
          <ReadOutlined /> <FormattedMessage id="pages.bookReader.limits.pages.title" />
        </div>

        {ScraperTypes.map((type) => {
          const sourceContents = book.contents
            .filter((c) => c.source === type)
            .sort((a, b) => b.id.localeCompare(a.id));
          const linkContent = content.source === type ? content : selectContent(sourceContents);

          if (!linkContent) return null;

          return <SourceItem key={type} type={type} content={linkContent} />;
        })}

        <SupportItem />
      </div>
    </animated.div>
  );
};

const Item = ({ icon, name, description }: { icon?: ReactNode; name?: ReactNode; description?: ReactNode }) => {
  const [hover, setHover] = useState(false);

  const arrow = useSpring({
    color: hover ? getColor("black").rgb : getColor("gray", "darker").rgb,
  });

  return (
    <div className="p-4 flex flex-row w-full" onMouseEnter={() => setHover(true)} onMouseLeave={() => setHover(false)}>
      <div className="flex flex-shrink-0 mr-2">
        <div className="my-auto">{icon}</div>
      </div>

      <div className="flex flex-1 flex-col align-middle min-w-0">
        <div className="font-bold">{name}</div>
        <div className="text-xs text-gray-darker truncate">{description}</div>
      </div>

      <animated.div style={arrow} className="flex flex-shrink-0 ml-4">
        <ArrowRightOutlined className="my-auto" />
      </animated.div>
    </div>
  );
};

const SourceItem = ({ type, content }: { type: ScraperType; content: BookContent }) => {
  const {
    info: { scrapers },
  } = useClientInfo();

  return (
    <Anchor className="block" target="_blank" href={content.sourceUrl}>
      <Item
        icon={<SourceIcon type={type} className="w-8 h-8 rounded-full" />}
        name={scrapers.find((s) => s.type === type)?.name}
        description={content.sourceUrl}
      />
    </Anchor>
  );
};

const SupportItem = () => {
  return (
    <SupportLink className="block">
      <Item
        icon={
          <HeartFilled
            className={cx(
              "text-pink",
              css`
                font-size: 2em;
              `
            )}
          />
        }
        name={<FormattedMessage id="pages.bookReader.limits.pages.support.name" />}
        description={<FormattedMessage id="pages.bookReader.limits.pages.support.description" />}
      />
    </SupportLink>
  );
};
