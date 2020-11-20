const fs = require('fs')
const path = require('path')
const { execSync: execSync2 } = require('child_process')

const execSync = (...x) => {
  process.stdout.write(execSync2(...x))
}

const emptyDirSync = function (dir) {
  if (fs.existsSync(dir)) {
    for (const filename of fs.readdirSync(dir)) {
      const file = path.join(dir, filename)

      if (fs.lstatSync(file).isDirectory())
        emptyDirSync(file)
      else
        fs.unlinkSync(file)
    }
  }
}

let language = process.argv[2]
let source = process.argv[3]

switch (source) {
  case undefined:
    source = 'https://nhitomi.chiya.dev/api/v1/docs.json'
    break

  case 'local':
    source = 'http://localhost:5000/api/v1/docs.json'
    break
}

const name = 'nhitomi-api'

emptyDirSync(name)

execSync(
  `yarn openapi-generator-cli generate \
    -psupportsES6=true \
    -ptypescriptThreePlus=true \
    -pprefixParameterInterfaces=true \
    -pnpmName=${name} \
    -pnpmRepository=https://github.com/chiyadev/nhitomi \
    -i ${source} \
    -g ${language} \
    -o ${name}`
)

execSync(`cd ${name} && yarn && yarn build`)
execSync(`yarn add link:${name} --optional`)
