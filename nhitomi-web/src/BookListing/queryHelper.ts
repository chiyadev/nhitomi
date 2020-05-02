import { BookQuery, BookTag, QueryMatchMode, BookQueryTags } from '../Client'

function convertValue(value: string) {
  if (value.startsWith('"') && value.endsWith('"'))
    value = value.slice(1, -1)

  // tag query is a full-text search (words are OR'ed)
  // wrap value in double quotes to use phrase query instead (words are AND'ed)
  return `"${value.toLowerCase()}"`
}

export function addQueryTag(query: BookQuery, tag: BookTag, value: string): BookQuery {
  value = convertValue(value)

  return {
    ...query,
    tags: {
      ...query.tags,
      [tag]: {
        mode: QueryMatchMode.All,
        values: [
          ...query.tags?.[tag]?.values || [],
          value
        ].filter((v, i, a) => a.indexOf(v) === i) // remove duplicates
      }
    }
  }
}

export function removeQueryTag(query: BookQuery, tag: BookTag, value: string): BookQuery {
  value = convertValue(value)

  const values = query.tags?.[tag]?.values.filter(v => v !== value)

  return {
    ...query,
    tags: {
      ...query.tags,
      [tag]: values?.length
        ? {
          mode: QueryMatchMode.All,
          values
        }
        : undefined
    }
  }
}

export function toggleQueryTag(query: BookQuery, tag: BookTag, value: string): BookQuery {
  value = convertValue(value)

  if (query.tags?.[tag]?.values.includes(value))
    return removeQueryTag(query, tag, value)

  return addQueryTag(query, tag, value)
}

export function setQueryTags(query: BookQuery, tags: { tag: BookTag, value: string }[]): BookQuery {
  return {
    ...query,
    tags: tags.reduce((a, { tag, value }) => {
      value = convertValue(value)

      if (a[tag]) {
        a[tag]!.values.push(value)
      }
      else {
        a[tag] = {
          mode: QueryMatchMode.All,
          values: [value]
        }
      }

      return a
    }, {} as BookQueryTags)
  }
}

export function flattenQueryTags(query: BookQuery): { tag: BookTag, value: string }[] {
  const tags = Object.keys(query.tags || {}) as BookTag[]

  return tags.flatMap(tag => query.tags?.[tag]?.values?.map(v => ({ tag, value: v.slice(1, -1) })) || [])
}
