import React from 'react'
import { RoundIconButton } from '../Components/RoundIconButton'
import { CurrentLocaleFlag } from '../Components/LocaleFlag'
import { SortDescendingOutlined, SortAscendingOutlined } from '@ant-design/icons'
import { useUrlState } from '../url'
import { SearchQuery } from './search'
import { SortDirection } from 'nhitomi-api'
import { SettingsLink } from '../Settings'

export const Menu = () => {
  const [query] = useUrlState<SearchQuery>()

  return (
    <div className='clearfix'>
      <ul className='float-right px-2'>
        <li className='inline-block'>
          <SettingsLink focus='language'>
            <RoundIconButton>
              <CurrentLocaleFlag />
            </RoundIconButton>
          </SettingsLink>
        </li>

        <li className='inline-block'>
          <RoundIconButton>
            {query.order === SortDirection.Ascending ? <SortAscendingOutlined /> : <SortDescendingOutlined />}
          </RoundIconButton>
        </li>
      </ul>
    </div >
  )
}
