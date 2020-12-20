import { createContext, Dispatch, SetStateAction, useContext } from "react";

export type ReaderScrollState = {
  // currently visible things
  currentPage: number;
  currentRow: number;

  // things that will become visible when state is set (thus "induced")
  inducedPage?: number;
  inducedRow?: number;
};

export const ReaderScrollContext = createContext<[ReaderScrollState, Dispatch<SetStateAction<ReaderScrollState>>]>([
  {
    currentRow: 0,
    currentPage: 0,
  },
  () => {},
]);

export function useReaderScroll() {
  return useContext(ReaderScrollContext);
}
