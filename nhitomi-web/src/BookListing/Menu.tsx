import React from 'react'
import { RoundIconButton } from '../Components/RoundIconButton'
import { CurrentLocaleFlag } from '../Components/LocaleFlag'
import { SortDescendingOutlined, SortAscendingOutlined } from '@ant-design/icons'
import { useQueryState } from '../state'
import { SearchQuery } from './search'
import { SortDirection, BookSort } from 'nhitomi-api'
import { SettingsLink } from '../Settings'
import { Tooltip } from '../Components/Tooltip'
import { LanguageNames } from '../LocaleManager'
import { DropdownGroup, Dropdown, DropdownItem } from '../Components/Dropdown'
import { BookListingLink } from '.'
import { FormattedMessage } from 'react-intl'
import { CheckBox } from '../Components/Checkbox'

export const LanguageButton = () => {
  const [query] = useQueryState<SearchQuery>()

  return (
    <Tooltip placement='bottom' overlay={(
      <div>
        {query.langs?.map(language => (
          <div>{LanguageNames[language]}</div>
        ))}
      </div>
    )}>

      <SettingsLink focus='language'>
        <RoundIconButton>
          <CurrentLocaleFlag />
        </RoundIconButton>
      </SettingsLink>
    </Tooltip>
  )
}

export const SortButton = () => {
  const [query] = useQueryState<SearchQuery>()

  return (
    <Dropdown placement='bottom' overlay={<>
      <DropdownGroup name={<FormattedMessage id='pages.bookListing.menu.order' />}>
        {Object.values(SortDirection).map(direction => (
          <BookListingLink mode='replace' query={{ ...query, order: direction }}>
            <DropdownItem padding={false}>
              <CheckBox type='radio' value={query.order === direction}>
                <FormattedMessage id={`types.sortDirection.${direction}`} />
              </CheckBox>
            </DropdownItem>
          </BookListingLink>
        ))}
      </DropdownGroup>

      <DropdownGroup name={<FormattedMessage id='pages.bookListing.menu.sort' />}>
        {Object.values(BookSort).map(sort => (
          <BookListingLink mode='replace' query={{ ...query, sort }}>
            <DropdownItem padding={false}>
              <CheckBox type='radio' value={query.sort === sort}>
                <FormattedMessage id={`types.bookSort.${sort}`} />
              </CheckBox>
            </DropdownItem>
          </BookListingLink>
        ))}
      </DropdownGroup>
    </>}>

      <RoundIconButton className='cursor-pointer'>
        {query.order === SortDirection.Ascending
          ? <SortAscendingOutlined />
          : <SortDescendingOutlined />}
      </RoundIconButton>
    </Dropdown>
  )
}
