using System;

namespace nhitomi.Models.Requests
{
    public class RestrictUserRequest
    {
        /// <summary>
        /// Duration of the restriction. Null implies indefinite restriction.
        /// </summary>
        public TimeSpan? Duration { get; set; }
    }
}