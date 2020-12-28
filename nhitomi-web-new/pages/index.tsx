import { GetStaticProps } from "next";

type Props = {};

export const getStaticProps: GetStaticProps<Props> = async ({ locale, defaultLocale }) => {
  let prefix = "";

  if (locale !== defaultLocale) {
    prefix = `/${locale}`;
  }

  return {
    redirect: {
      destination: prefix + "/books",
      permanent: false,
    },
  };
};

const Index = () => null;

export default Index;
