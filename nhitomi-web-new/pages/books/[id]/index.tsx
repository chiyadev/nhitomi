import { memo } from "react";
import { GetServerSideProps } from "next";
import { createApiClient } from "../../../utils/client";
import { selectContent } from "../../../utils/book";
import { parseCookies } from "nookies";
import { sanitizeProps } from "../../../utils/props";

type Props = {
  error?: Error;
};

export const getServerSideProps: GetServerSideProps<Props> = async ({ req, res, query: { id } }) => {
  try {
    const client = createApiClient(req);

    if (!client) {
      return {
        redirect: {
          destination: "/auth",
          permanent: false,
          statusCode: 401,
        },
      };
    }

    const book = await client.book.getBook({
      id: Array.isArray(id) ? id[0] : id,
    });

    const content = selectContent(book.contents, parseCookies({ req }));

    return {
      redirect: {
        destination: `/books/${book.id}/contents/${content.id}`,
        permanent: false,
      },
    };
  } catch (e) {
    res.statusCode = 400;

    return {
      props: sanitizeProps({
        error: e,
      }),
    };
  }
};

const BookRedirect = ({}: Props) => {
  return null;
};

export default memo(BookRedirect);
