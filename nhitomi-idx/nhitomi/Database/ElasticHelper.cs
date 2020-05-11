using System;
using System.Collections.Generic;
using System.Linq;
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
            public QueryMatchMode Mode = QueryMatchMode.All;

            public QueryWrapper(QueryContainerDescriptor<T> descriptor)
            {
                Descriptor = descriptor;
            }

            /// <summary>
            /// Sets the match mode for this query, which is by default <see cref="QueryMatchMode.All"/>.
            /// This should be called before adding any other queries.
            /// </summary>
            public QueryWrapper<T> SetMode(QueryMatchMode mode)
            {
                Mode = mode;
                return this;
            }

            /// <summary>
            /// Adds a raw query container.
            /// </summary>
            public QueryWrapper<T> AddQuery(Func<QueryContainerDescriptor<T>, QueryContainer> createContainer)
            {
                var c = createContainer(Descriptor);

                if (c != null)
                {
                    if (Container == null)
                        Container = c;
                    else
                        switch (Mode)
                        {
                            case QueryMatchMode.Any:
                                Container |= c;
                                break;
                            case QueryMatchMode.All:
                                Container &= c;
                                break;
                        }
                }

                return this;
            }
        }

        /// <summary>
        /// Convenience function for adding multiple queries to a query container.
        /// </summary>
        /// <param name="searchDesc">Search descriptor.</param>
        /// <param name="query">Query builder.</param>
        public static SearchDescriptor<T> MultiQuery<T>(this SearchDescriptor<T> searchDesc, Func<QueryWrapper<T>, QueryWrapper<T>> query) where T : class
            => searchDesc.Query(q => q.Bool(b => b.Must(m => query(new QueryWrapper<T>(m)).Container ?? m)));

        /// <summary>
        /// Nested equivalent of <see cref="MultiQuery{T}"/>.
        /// </summary>
        public static QueryWrapper<T> Nested<T>(this QueryWrapper<T> wrapper, Func<QueryWrapper<T>, QueryWrapper<T>> query) where T : class
            => wrapper.AddQuery(q => query(new QueryWrapper<T>(q)).Container);

        /// <summary>
        /// Adds <see cref="TextQuery"/> to query.
        /// </summary>
        /// <param name="wrapper">Query wrapper.</param>
        /// <param name="query">Query object.</param>
        /// <param name="paths">Expression path to querying field.</param>
        public static QueryWrapper<T> Text<T>(this QueryWrapper<T> wrapper, TextQuery query, params Expression<Func<T, object>>[] paths) where T : class
        {
            if (query == null || !query.IsSpecified)
                return wrapper;

            return wrapper.AddQuery(descriptor =>
            {
                var container = null as QueryContainer;

                foreach (var value in query.Values)
                {
                    if (string.IsNullOrEmpty(value))
                        continue;

                    var c = descriptor.SimpleQueryString(q => q.Query(value).Fields(f => paths.Aggregate(f, (ff, p) => ff.Field(p))).DefaultOperator(wrapper.Mode == QueryMatchMode.All ? Operator.And : Operator.Or));

                    if (container == null)
                        container = c;
                    else
                        switch (query.Mode)
                        {
                            case QueryMatchMode.Any:
                                container |= c;
                                break;
                            case QueryMatchMode.All:
                                container &= c;
                                break;
                        }
                }

                return container;
            });
        }

        /// <summary>
        /// Adds <see cref="FilterQuery{T}"/> to query.
        /// </summary>
        /// <param name="wrapper">Query wrapper.</param>
        /// <param name="query">Query object.</param>
        /// <param name="path">Expression path to querying field.</param>
        public static QueryWrapper<T> Filter<T, TField>(this QueryWrapper<T> wrapper, FilterQuery<TField> query, Expression<Func<T, object>> path) where T : class
        {
            if (query == null || !query.IsSpecified)
                return wrapper;

            return wrapper.AddQuery(descriptor =>
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
                            case QueryMatchMode.Any:
                                container |= c;
                                break;
                            case QueryMatchMode.All:
                                container &= c;
                                break;
                        }
                }

                return container;
            });
        }

        /// <summary>
        /// Adds <see cref="RangeQuery{T}"/> to query.
        /// Only <see cref="int"/>, <see cref="double"/> and <see cref="DateTime"/> are supported.
        /// </summary>
        /// <param name="wrapper">Query wrapper.</param>
        /// <param name="query">Query object.</param>
        /// <param name="path">Expression path to querying field.</param>
        public static QueryWrapper<T> Range<T, TField>(this QueryWrapper<T> wrapper, RangeQuery<TField> query, Expression<Func<T, object>> path) where T : class where TField : struct
        {
            if (query == null || !query.IsSpecified)
                return wrapper;

            return wrapper.AddQuery(descriptor => query switch
            {
                RangeQuery<DateTime> r => descriptor.DateRange(q =>
                {
                    q = q.Field(path);

                    if (r.Minimum != null)
                        q = r.Exclusive ? q.GreaterThan(r.Minimum.Value) : q.GreaterThanOrEquals(r.Minimum.Value);

                    if (r.Maximum != null)
                        q = r.Exclusive ? q.LessThan(r.Maximum.Value) : q.LessThanOrEquals(r.Maximum.Value);

                    return q;
                }),

                RangeQuery<int> r => descriptor.Range(q =>
                {
                    q = q.Field(path);

                    if (r.Minimum != null)
                        q = r.Exclusive ? q.GreaterThan(r.Minimum.Value) : q.GreaterThanOrEquals(r.Minimum.Value);

                    if (r.Maximum != null)
                        q = r.Exclusive ? q.LessThan(r.Maximum.Value) : q.LessThanOrEquals(r.Maximum.Value);

                    return q;
                }),

                RangeQuery<double> r => descriptor.Range(q =>
                {
                    q = q.Field(path);

                    if (r.Minimum != null)
                        q = r.Exclusive ? q.GreaterThan(r.Minimum.Value) : q.GreaterThanOrEquals(r.Minimum.Value);

                    if (r.Maximum != null)
                        q = r.Exclusive ? q.LessThan(r.Maximum.Value) : q.LessThanOrEquals(r.Maximum.Value);

                    return q;
                }),

                _ => throw new NotSupportedException($"Unsupported range query type {typeof(T)}.")
            });
        }

        public delegate (SortDirection, Expression<Func<T, object>>) SortingFieldPathDelegate<T>();

        /// <summary>
        /// Adds sorting to query using the specified paths.
        /// </summary>
        /// <param name="searchDesc">Search descriptor.</param>
        /// <param name="paths">Sorting paths and directions. Returning null implies sorting by "relevance".</param>
        public static SearchDescriptor<T> MultiSort<T>(this SearchDescriptor<T> searchDesc, params SortingFieldPathDelegate<T>[] paths) where T : class
        {
            if (paths == null || paths.Length == 0)
                return searchDesc;

            return searchDesc.Sort(s =>
            {
                foreach (var path in paths)
                {
                    var (direction, expr) = path();

                    s.Field(f =>
                    {
                        // null implies relevance sorting
                        f = expr == null
                            ? f.Field("_score")
                            : f.Field(expr);

                        // ordering
                        f = direction == SortDirection.Ascending
                            ? f.Ascending()
                            : f.Descending();

                        return f;
                    });
                }

                return s;
            });
        }

        public delegate Expression<Func<T, object>> SortingFieldPathDelegate<T, in TSort>(TSort attribute);

        /// <summary>
        /// Adds sorting to query using the specified fields and field path converter.
        /// </summary>
        /// <param name="searchDesc">Search descriptor.</param>
        /// <param name="fields">Sorting fields.</param>
        /// <param name="path">Sorting field to expression path converter. Returning null implies sorting by "relevance".</param>
        public static SearchDescriptor<T> MultiSort<T, TSort>(this SearchDescriptor<T> searchDesc, IReadOnlyCollection<SortField<TSort>> fields, SortingFieldPathDelegate<T, TSort> path) where T : class where TSort : struct
        {
            if (fields == null || fields.Count == 0)
                return searchDesc;

            return searchDesc.Sort(s =>
            {
                var set = new HashSet<TSort>();

                foreach (var field in fields)
                {
                    // ensure no duplicate fields
                    if (!set.Add(field.Value))
                        continue;

                    var expr = path(field.Value);

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