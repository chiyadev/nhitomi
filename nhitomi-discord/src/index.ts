console.log('hello, world');

(async () => {
  for (; ;) {
    await new Promise(r => setTimeout(r, 1000))

    console.log('hello, fewf')
  }

})()
