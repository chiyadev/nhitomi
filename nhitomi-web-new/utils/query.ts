import { useRouter } from "next/router";
import { useCallback, useMemo } from "react";

type Dispatch<T = any> = (value: T, mode?: "replace" | "push") => Promise<boolean>;

export function useQuery(key: string): [string | string[] | undefined, Dispatch<string | string[] | undefined>] {
  const router = useRouter();

  const value = router.query[key];
  const setValue = useCallback<Dispatch>(
    (value: string | string[] | undefined, mode) => {
      return (mode === "push" ? router.push : router.replace).call(
        router,
        {
          query: {
            ...router.query,
            [key]: typeof value === "undefined" ? [] : value,
          },
        },
        undefined,
        {
          shallow: mode !== "push",
        }
      );
    },
    [router, key]
  );

  return [value, setValue];
}

export function useQueryString(key: string): [string | undefined, Dispatch<string | undefined>] {
  let [value, setValue] = useQuery(key);

  if (Array.isArray(value)) {
    value = value[0];
  }

  return [value, setValue];
}

export function useQueryBoolean(key: string): [boolean, Dispatch<boolean | undefined>] {
  const [valueRaw, setValueRaw] = useQueryString(key);

  const value = useMemo(() => {
    switch (valueRaw?.toLowerCase()) {
      case "":
      case "1":
      case "true":
        return true;

      default:
        return false;
    }
  }, [valueRaw]);

  const setValue = useCallback<Dispatch>(
    (value: boolean | undefined, mode) => {
      return setValueRaw(value ? "1" : undefined, mode);
    },
    [setValueRaw]
  );

  return [value, setValue];
}
