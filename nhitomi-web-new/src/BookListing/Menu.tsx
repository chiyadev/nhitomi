import React from 'react'
import { RoundIconButton } from '../Components/RoundIconButton'
import { CurrentLocaleFlag } from '../Components/LocaleFlag'
import { SortDescendingOutlined, SortAscendingOutlined } from '@ant-design/icons'
import { useQueryState } from '../state'
import { SearchQuery } from './search'
import { SortDirection } from 'nhitomi-api'
import { SettingsLink } from '../Settings'
import { useSpring, animated } from 'react-spring'
import { Tooltip } from '../Components/Tooltip'
import { LanguageNames } from '../LocaleManager'

export const Menu = () => {
  const [query] = useQueryState<SearchQuery>()

  const iconStyle = useSpring({
    from: { opacity: 0, transform: 'scale(0.9)' },
    to: { opacity: 1, transform: 'scale(1)' }
  })

  return (
    <div className='clearfix'>
      <ul className='float-right px-2'>
        <animated.li style={iconStyle} className='inline-block'>
          <LanguageButton query={query} />
        </animated.li>

        <animated.li style={iconStyle} className='inline-block'>
          <RoundIconButton>
            {query.order === SortDirection.Ascending ? <SortAscendingOutlined /> : <SortDescendingOutlined />}
          </RoundIconButton>
        </animated.li>
      </ul>
    </div >
  )
}

const LanguageButton = ({ query }: { query: SearchQuery }) => {
  return (
    <Tooltip placement='bottom' overlay={(
      <ul>
        {query.langs?.map(language => (
          <li>{LanguageNames[language]}</li>
        ))}
      </ul>
    )}>

      <SettingsLink focus='language'>
        <RoundIconButton>
          <CurrentLocaleFlag />
        </RoundIconButton>
      </SettingsLink>
    </Tooltip>
  )
}
