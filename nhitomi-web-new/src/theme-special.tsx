export { }

function css(s: TemplateStringsArray) {
  const style = document.createElement('style')
  style.type = 'text/css'
  style.innerText = s.toString()

  document.head.appendChild(style)
}

// add beautiful frosty backgrounds for all browsers that support it! *looks with disgust at firefox*
if (CSS.supports('backdrop-filter', 'blur(0)')) {
  css`
    .bg-blur {
      backdrop-filter: blur(0.5em);
      --bg-opacity: 80%;
    }
  `
}
