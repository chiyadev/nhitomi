export function getServerSideProps() {
  return {
    props: {},
    redirect: {
      destination: "/books",
      permanent: false,
    },
  };
}

const Index = () => null;

export default Index;
