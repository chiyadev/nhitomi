import React, { ReactNode, useLayoutEffect, useMemo, useState } from "react";
import { PrefetchGenerator, PrefetchLink, TypedPrefetchLinkProps, usePostfetch } from "../Prefetch";
import { PageContainer } from "../Components/PageContainer";
import { useTabTitle } from "../TitleSetter";
import { useLocalized } from "../LocaleManager";
import { Container } from "../Components/Container";
import { useDownloads } from "../DownloadManager";
import { FormattedMessage } from "react-intl";
import { cx } from "emotion";
import { BookTaskDisplay } from "./BookTaskDisplay";
import { useLayout } from "../LayoutManager";
import { EmptyIndicator } from "../Components/EmptyIndicator";
import { Menu } from "./Menu";
import { animated, useSpring } from "react-spring";

export type PrefetchResult = {};
export type PrefetchOptions = {};

export const useDownloadsPrefetch: PrefetchGenerator<PrefetchResult, PrefetchOptions> = () => {
  return {
    destination: {
      path: "/downloads",
    },

    fetch: async () => ({}),
  };
};

export const DownloadsLink = ({ ...props }: TypedPrefetchLinkProps & PrefetchOptions) => (
  <PrefetchLink fetch={useDownloadsPrefetch} options={{}} {...props} />
);

export const Downloads = (options: PrefetchOptions) => {
  const { result } = usePostfetch(useDownloadsPrefetch, {
    requireAuth: true,
    ...options,
  });

  if (!result) return null;

  return (
    <PageContainer>
      <Loaded {...result} />
    </PageContainer>
  );
};

const Loaded = () => {
  useTabTitle(useLocalized("pages.downloads.title"));

  const { screen } = useLayout();
  const { tasks } = useDownloads();

  const [running, setRunning] = useState(0);

  useLayoutEffect(() => {
    const handle = () => {
      setRunning(tasks.filter((t) => t.state.type === "running").length);
    };

    for (const task of tasks) task.on("updated", handle);

    return () => {
      for (const task of tasks) task.off("updated", handle);
    };
  }, [tasks]);

  return (
    <Container className="divide-y divide-gray-darkest">
      {useMemo(
        () => (
          <div className="p-4">
            <div className="text-2xl">
              <FormattedMessage id="pages.downloads.title" />
            </div>
            <div className="text-sm text-gray-darker">
              <FormattedMessage id="pages.downloads.subtitle" values={{ count: running }} />
            </div>
          </div>
        ),
        [running]
      )}

      <div>
        <MenuWrapper>{!!tasks.length && <Menu />}</MenuWrapper>

        {!tasks.length && (
          <div className="p-4">
            <EmptyIndicator>
              <FormattedMessage id="pages.downloads.empty" />
            </EmptyIndicator>
          </div>
        )}

        {useMemo(
          () =>
            tasks.map((task) => {
              let node: ReactNode;

              switch (task.target.type) {
                case "book":
                  node = <BookTaskDisplay task={task} />;
                  break;
              }

              return (
                <div
                  key={task.id}
                  className={cx({ "w-1/2": screen === "lg", "w-full": screen !== "lg" }, "inline-flex")}
                >
                  {node}
                </div>
              );
            }),
          [tasks]
        )}
      </div>
    </Container>
  );
};

const MenuWrapper = ({ children }: { children?: ReactNode }) => {
  const style = useSpring({
    from: { opacity: 0 },
    to: { opacity: 1 },
  });

  return (
    <animated.div style={style} className="w-full flex flex-row justify-end px-2 pt-4">
      {children}
    </animated.div>
  );
};
