// https://www.safari-is-the-new-ie.com/
// https://github.com/necolas/react-native-web/issues/1369#issuecomment-502476921
// https://hackernoon.com/onresize-event-broken-in-mobile-safari-d8469027bf4d

export const isSafari = /^((?!chrome|android).)*safari/i.test(navigator.userAgent);
export const safariResizeDelay = 500;
