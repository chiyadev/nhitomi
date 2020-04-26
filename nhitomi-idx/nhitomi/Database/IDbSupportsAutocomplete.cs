using Nest;

namespace nhitomi.Database
{
    public interface IDbSupportsAutocomplete
    {
        public const string SuggestField = "sug";

        CompletionField Suggest { get; set; }
    }
}