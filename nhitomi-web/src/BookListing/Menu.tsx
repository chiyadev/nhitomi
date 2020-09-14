import React from 'react'
import { RoundIconButton } from '../Components/RoundIconButton'
import { CurrentLocaleFlag } from '../Components/LocaleFlag'
import { SortDescendingOutlined, SortAscendingOutlined, FilterOutlined, FilterFilled } from '@ant-design/icons'
import { useQueryState } from '../state'
import { SearchQuery } from './search'
import { SortDirection, BookSort, ScraperType } from 'nhitomi-api'
import { SettingsLink } from '../Settings'
import { Tooltip } from '../Components/Tooltip'
import { LanguageNames } from '../LocaleManager'
import { DropdownGroup, Dropdown, DropdownItem } from '../Components/Dropdown'
import { BookListingLink } from '.'
import { FormattedMessage } from 'react-intl'
import { CheckBox } from '../Components/Checkbox'
import { useSpring, animated } from 'react-spring'
import { useClientInfo } from '../ClientManager'

export const Menu = () => <>
  <LanguageButton />
  <SortButton />
  <FilterButton />
</>

const LanguageButton = () => {
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

const SortButton = () => {
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

      <RoundIconButton>
        {query.order === SortDirection.Ascending
          ? <SortAscendingOutlined />
          : <SortDescendingOutlined />}
      </RoundIconButton>
    </Dropdown>
  )
}

const FilterButton = () => {
  const { info } = useClientInfo()
  const [query] = useQueryState<SearchQuery>()
  const filterActive = query.sources?.length

  const iconStyle = useSpring({
    opacity: filterActive ? 1 : 0.5
  })

  return (
    <Dropdown placement='bottom' overlay={<>
      <DropdownGroup name={<FormattedMessage id='pages.bookListing.menu.sources' />}>
        {info.scrapers.filter(s => s.type !== ScraperType.Unknown).map(({ name, type }) => {
          const active = query.sources && query.sources.indexOf(type) !== -1

          return (
            <BookListingLink mode='replace' query={{ ...query, sources: active ? query.sources!.filter(s => s !== type) : [...(query.sources || []), type] }}>
              <DropdownItem padding={false}>
                <CheckBox type='check' value={active}>
                  <img className='rounded-full h-5 w-auto inline' alt={type} src={`/assets/icons/${type}.jpg`} />
                  {' '}
                  {name}
                </CheckBox>
              </DropdownItem>
            </BookListingLink>
          )
        })}
      </DropdownGroup>
    </>}>

      <RoundIconButton>
        <animated.span style={iconStyle}>
          {filterActive
            ? <FilterFilled />
            : <FilterOutlined />}
        </animated.span>
      </RoundIconButton>
    </Dropdown>
  )
}
