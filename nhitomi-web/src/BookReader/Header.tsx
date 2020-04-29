import { PageHeader } from 'antd'
import React from 'react'
import { useHistory } from 'react-router-dom'
import { BookReaderLink } from '.'
import { BookListingLink } from '../BookListing'
import { Book, BookContent } from '../Client'

export const Header = ({ book, content }: { book: Book, content: BookContent }) => {
  const { goBack } = useHistory()

  return <PageHeader
    onBack={goBack}
    title={book.primaryName}
    subTitle={book.englishName}
    breadcrumb={{
      routes: [{
        path: 'listing',
        breadcrumbName: 'Books'
      }, {
        path: 'book',
        breadcrumbName: book.primaryName
      }],
      itemRender: ({ path, breadcrumbName }) => {
        switch (path) {
          case 'listing': return <BookListingLink>{breadcrumbName}</BookListingLink>
          case 'book': return <BookReaderLink id={book.id} contentId={content.id}>{breadcrumbName}</BookReaderLink>
        }
      }
    }} />
}
