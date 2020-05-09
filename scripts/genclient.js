const fs = require('fs')
const path = require('path')
const { exec } = require('child_process')
const { promisify } = require('util')

exec2 = (...x) => {
  console.log(x[0])

  const p = exec(...x)
  p.stdout.pipe(process.stdout)
  return p
}

const rmdirSyncRecurse = function (dir) {
  if (!fs.existsSync(dir))
    return

  for (const filename of fs.readdirSync(dir)) {
    const file = path.join(dir, filename)

    if (fs.lstatSync(file).isDirectory())
      rmdirSyncRecurse(file)
    else
      fs.unlinkSync(file)
  }

  fs.rmdirSync(dir)
}

let source = process.argv[2]

switch (source) {
  case undefined:
    source = 'https://nhitomi-next.chiya.dev/api/v1/docs.json'
    break

  case 'local':
    source = 'http://localhost:5000/api/v1/docs.json'
    break
}

const name = 'nhitomi-api';

(async () => {
  rmdirSyncRecurse(name)

  await promisify(exec2)(
    `npx openapi-generator generate \
      -psupportsES6=true \
      -ptypescriptThreePlus=true \
      -pprefixParameterInterfaces=true \
      -pnpmName=${name} \
      -pnpmRepository=https://github.com/chiyadev/nhitomi \
      -i ${source} \
      -g typescript-fetch \
      -o ${name}`)

  await promisify(exec2)(`cd ${name} && npm run build`)
  await promisify(exec2)(`npm install --no-save ${name}`)
})()
