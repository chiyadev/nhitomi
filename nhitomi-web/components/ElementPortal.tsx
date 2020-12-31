import React, {
  createContext,
  Dispatch,
  Fragment,
  memo,
  MutableRefObject,
  ReactNode,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
} from "react";

type ContextType = {
  keys: MutableRefObject<number>;
  nodes: Map<number, ReactNode>;
  render: () => void;
};

const Context = createContext<ContextType>({
  keys: { current: 0 },
  nodes: new Map(),
  render: () => {},
});

const ElementPortalProvider = ({ children, onRender }: { children?: ReactNode; onRender: Dispatch<ReactNode> }) => {
  const keys = useRef(0);
  const nodes = useMemo(() => new Map<number, ReactNode>(), []);

  const render = useCallback(() => {
    const keys = Array.from(nodes.keys()).sort((a, b) => a - b);
    const result: ReactNode[] = [];

    for (const key of keys) {
      result.push(<Fragment key={key}>{nodes.get(key)}</Fragment>);
    }

    onRender(<>{result}</>);
  }, [nodes, onRender]);

  return (
    <Context.Provider value={useMemo(() => ({ keys, nodes, render }), [keys, nodes, render])}>
      {children}
    </Context.Provider>
  );
};

const ElementPortalConsumer = ({ children }: { children?: ReactNode }) => {
  const { keys, nodes, render } = useContext(Context);
  const key = useMemo(() => ++keys.current, [keys]);

  useEffect(() => {
    nodes.set(key, children);
    render();

    return () => {
      nodes.delete(key);
      render();
    };
  }, [key, children, nodes, render]);

  return null;
};

export default {
  Provider: memo(ElementPortalProvider),
  Consumer: memo(ElementPortalConsumer),
};
