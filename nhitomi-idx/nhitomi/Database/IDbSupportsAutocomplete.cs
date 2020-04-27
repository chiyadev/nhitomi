using Nest;

namespace nhitomi.Database
{
    public interface IDbSupportsAutocomplete
    {
        /// <summary>
        /// Autocomplete completion fields must be named "sug".
        /// </summary>
        [Completion(Name = "sug")]
        CompletionField Suggest { get; set; }
    }
}