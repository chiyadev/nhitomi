using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace nhitomi
{
    public class ModelSanitizerModelBinderProvider : IModelBinderProvider
    {
        readonly IModelBinderProvider[] _providers;

        public ModelSanitizerModelBinderProvider(IEnumerable<IModelBinderProvider> providers)
        {
            _providers = providers.ToArray();
        }

        public IModelBinder GetBinder(ModelBinderProviderContext context)
            => _providers.Select(p => p.GetBinder(context))
                         .Where(b => b != null)
                         .Select(b => new ModelSanitizerModelBinder(b))
                         .FirstOrDefault();
    }

    public class ModelSanitizerModelBinder : IModelBinder
    {
        readonly IModelBinder _binder;

        public ModelSanitizerModelBinder(IModelBinder binder)
        {
            _binder = binder;
        }

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            await _binder.BindModelAsync(bindingContext);

            if (bindingContext.Result.IsModelSet)
                bindingContext.Result = ModelBindingResult.Success(ModelSanitizer.Sanitize(bindingContext.Result.Model));
        }
    }
}