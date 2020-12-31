import { GetServerSideProps } from "next";
import { parseCookies } from "nookies";
import { parseConfigs } from "../utils/config";

type Props = {};

export const getServerSideProps: GetServerSideProps<Props> = async (ctx) => {
  const cookies = parseCookies(ctx);
  const { token } = parseConfigs(cookies);

  if (token) {
    return {
      redirect: {
        destination: "/books",
        permanent: false,
      },
    };
  } else {
    return {
      redirect: {
        destination: "/auth",
        permanent: false,
      },
    };
  }
};

const Index = () => null;
export default Index;
