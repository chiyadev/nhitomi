const { emptyDir } = require('fs-extra')
const { exec } = require('child_process')
const { promisify } = require('util')

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
  // delete existing files
  await emptyDir(`./${name}`)

  // generate client
  console.log('Generating client using spec:', source)

  await promisify(exec)(
    `npx openapi-generator generate \
      -psupportsES6=true \
      -ptypescriptThreePlus=true \
      -pprefixParameterInterfaces=true \
      -pnpmName=${name} \
      -pnpmRepository=https://github.com/chiyadev/nhitomi \
      -i ${source} \
      -g typescript-fetch \
      -o ./${name}`)

  // build package
  console.log('Building package...')

  await promisify(exec)(`cd ./${name} && npm run build`)

  // save package
  console.log('Installing package...')

  await promisify(exec)(`npm install --no-save ./${name}`)
})()
