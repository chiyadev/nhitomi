using System;

namespace nhitomi.Database
{
    [Serializable]
    public class ConcurrencyException : ApplicationException
    {
        public ConcurrencyException(Exception inner) : base(inner?.Message, inner) { }
    }
}