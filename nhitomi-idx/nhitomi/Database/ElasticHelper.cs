using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Nest;
using nhitomi.Models.Queries;

namespace nhitomi.Database
{
    public static class ElasticHelper
    {
        public sealed class QueryWrapper<T> where T : class
        {
            public readonly QueryContainerDescriptor<T> Descriptor;

            public QueryContainer Container;

            public QueryWrapper(QueryContainerDescriptor<T> descriptor)
            {
                Descriptor = descriptor;
            }
        }

        /// <summary>
        /// Adds filtering to query through <see cref="Text{T}"/>, <see cref="Filter{T,TField}"/>and <see cref="Range{T,TField}"/> extensions.
        /// </summary>
        /// <param name="searchDesc">Search descriptor.</param>
        /// <param name="query">Query builder.</param>
        public static SearchDescriptor<T> MultiQuery<T>(this SearchDescriptor<T> searchDesc,
                                                        Func<QueryWrapper<T>, QueryWrapper<T>> query) where T : class
            => searchDesc.Query(q => q.Bool(bq =>
            {
                bq.Must(mq => query(new QueryWrapper<T>(mq)).Container ?? mq);
                /*boolQuery.Should(q => query(new QueryWrapper<T>(q, QueryStrictness.Should)).Container ?? q);
                boolQuery.Filter(q => query(new QueryWrapper<T>(q, QueryStrictness.Filter)).Container ?? q);*/

                return bq;
            }));

        static QueryWrapper<T> AddQuery<T>(this QueryWrapper<T> wrapper,
                                           IQueryComponent query,
                                           Func<QueryContainerDescriptor<T>, QueryContainer> createContainer) where T : class
        {
            if (query == null || !query.IsSpecified)
                return wrapper;

            var cont = createContainer(wrapper.Descriptor);

            if (cont != null)
            {
                if (wrapper.Container == null)
                    wrapper.Container = cont;
                else
                    wrapper.Container &= cont;
            }

            return wrapper;
        }

        /// <summary>
        /// Adds <see cref="TextQuery"/> to query.
        /// </summary>
        /// <param name="wrapper">Query wrapper.</param>
        /// <param name="query">Query object.</param>
        /// <param name="path">Expression path to querying field, or null to query against all fields.</param>
        public static QueryWrapper<T> Text<T>(this QueryWrapper<T> wrapper,
                                              TextQuery query,
                                              Expression<Func<T, object>> path = null) where T : class
            => wrapper.AddQuery(
                query,
                descriptor =>
                {
                    var container = null as QueryContainer;

                    foreach (var value in query.Values)
                    {
                        if (string.IsNullOrWhiteSpace(value))
                            continue;

                        /*switch (query.Mode)
                        {
                            default:*/

                        var c = descriptor.SimpleQueryString(q =>
                        {
                            // path = null signifies all fields
                            if (path != null)
                                q.Fields(f => f.Field(path));

                            q.Query(value.Trim());

                            return q;
                        });

/*                              break;

                            case TextQueryMode.Match:
                                if (paths.Length == 1)
                                    c = descriptor.Match(q => q.Field(paths[0])
                                                               .Query(value));
                                else
                                    c = descriptor.MultiMatch(q => q.Fields(paths)
                                                                    .Query(value));

                                break;

                            case TextQueryMode.Phrase:
                                c = descriptor.MatchPhrase(q => q.Field(paths[0]) // always use first
                                                                 .Query(value));

                                break;
                        }*/

                        if (container == null)
                            container = c;
                        else
                            switch (query.Mode)
                            {
                                default:
                                    container |= c;
                                    break;
                                case QueryMatchMode.All:
                                    container &= c;
                                    break;
                            }
                    }

                    return container;
                });

        /// <summary>
        /// Adds <see cref="FilterQuery{T}"/> to query.
        /// </summary>
        /// <param name="wrapper">Query wrapper.</param>
        /// <param name="query">Query object.</param>
        /// <param name="path">Expression path to querying field.</param>
        public static QueryWrapper<T> Filter<T, TField>(this QueryWrapper<T> wrapper,
                                                        FilterQuery<TField> query,
                                                        Expression<Func<T, object>> path) where T : class
            => wrapper.AddQuery(
                query,
                descriptor =>
                {
                    var container = null as QueryContainer;

                    foreach (var value in query.Values)
                    {
                        var c = descriptor.Term(t => t.Field(path).Value(value));

                        if (container == null)
                            container = c;
                        else
                            switch (query.Mode)
                            {
                                default:
                                    container |= c;
                                    break;
                                case QueryMatchMode.All:
                                    container &= c;
                                    break;
                            }
                    }

                    return container;
                });

        /// <summary>
        /// Adds <see cref="RangeQuery{T}"/> to query.
        /// Only <see cref="int"/>, <see cref="double"/> and <see cref="DateTime"/> are supported. Other types will not have any effect.
        /// </summary>
        /// <param name="wrapper">Query wrapper.</param>
        /// <param name="query">Query object.</param>
        /// <param name="path">Expression path to querying field.</param>
        public static QueryWrapper<T> Range<T, TField>(this QueryWrapper<T> wrapper,
                                                       RangeQuery<TField> query,
                                                       Expression<Func<T, object>> path) where T : class where TField : struct
            => wrapper.AddQuery(
                query,
                descriptor =>
                {
                    var container = null as QueryContainer;

                    //todo: how to DRY
                    switch (query)
                    {
                        case RangeQuery<DateTime> dateTimeRange:
                            container = descriptor.DateRange(q =>
                            {
                                q = q.Field(path);

                                if (dateTimeRange.Minimum != null)
                                    q = dateTimeRange.Exclusive
                                        ? q.GreaterThan(dateTimeRange.Minimum.Value)
                                        : q.GreaterThanOrEquals(dateTimeRange.Minimum.Value);

                                if (dateTimeRange.Maximum != null)
                                    q = dateTimeRange.Exclusive
                                        ? q.LessThan(dateTimeRange.Maximum.Value)
                                        : q.LessThanOrEquals(dateTimeRange.Maximum.Value);

                                return q;
                            });

                            break;

                        case RangeQuery<int> intRange:
                            container = descriptor.Range(q =>
                            {
                                q = q.Field(path);

                                if (intRange.Minimum != null)
                                    q = intRange.Exclusive
                                        ? q.GreaterThan(intRange.Minimum.Value)
                                        : q.GreaterThanOrEquals(intRange.Minimum.Value);

                                if (intRange.Maximum != null)
                                    q = intRange.Exclusive
                                        ? q.LessThan(intRange.Maximum.Value)
                                        : q.LessThanOrEquals(intRange.Maximum.Value);

                                return q;
                            });

                            break;

                        case RangeQuery<double> doubleRange:
                            container = descriptor.Range(q =>
                            {
                                q = q.Field(path);

                                if (doubleRange.Minimum != null)
                                    q = doubleRange.Exclusive
                                        ? q.GreaterThan(doubleRange.Minimum.Value)
                                        : q.GreaterThanOrEquals(doubleRange.Minimum.Value);

                                if (doubleRange.Maximum != null)
                                    q = doubleRange.Exclusive
                                        ? q.LessThan(doubleRange.Maximum.Value)
                                        : q.LessThanOrEquals(doubleRange.Maximum.Value);

                                return q;
                            });

                            break;
                    }

                    return container;
                });

        public delegate Expression<Func<T, object>> SortingFieldPathDelegate<T, in TSort>(TSort attribute);

        /// <summary>
        /// Adds sorting to query using the specified fields and field path converter.
        /// </summary>
        /// <param name="searchDesc">Search descriptor.</param>
        /// <param name="fields">Sorting fields.</param>
        /// <param name="path">Sorting field to expression path converter. Returning null implies sorting by "relevance".</param>
        public static SearchDescriptor<T> MultiSort<T, TSort>(this SearchDescriptor<T> searchDesc,
                                                              IReadOnlyList<SortField<TSort>> fields,
                                                              SortingFieldPathDelegate<T, TSort> path) where T : class where TSort : struct
        {
            if (fields == null || fields.Count == 0)
                return searchDesc;

            return searchDesc.Sort(s =>
            {
                var set = new HashSet<TSort>();

                foreach (var field in fields)
                {
                    // ensure not sorting by one field multiple times
                    if (!set.Add(field.Value))
                        continue;

                    Expression<Func<T, object>> expr;

                    try
                    {
                        expr = path(field.Value);
                    }
                    catch (ArgumentException)
                    {
                        continue;
                    }

                    s = s.Field(f =>
                    {
                        // null implies relevance sorting
                        f = expr == null
                            ? f.Field("_score")
                            : f.Field(expr);

                        // ordering
                        f = field.Direction == SortDirection.Ascending
                            ? f.Ascending()
                            : f.Descending();

                        return f;
                    });
                }

                return s;
            });
        }
    }
}