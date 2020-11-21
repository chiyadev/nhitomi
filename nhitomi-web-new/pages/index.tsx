import { GetStaticProps } from "next";
import { memo } from "react";

type Props = {};

export const getStaticProps: GetStaticProps<Props> = async () => {
  return {
    redirect: {
      destination: "/books",
      permanent: false,
    },
  };
};

const Index = () => null;

export default memo(Index);
