import { GetStaticProps } from "next";

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
export default Index;
