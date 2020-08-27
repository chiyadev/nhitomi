using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OneOf;
using OneOf.Types;

namespace nhitomi.Storage
{
    /// <summary>
    /// A dictionary of glob prefixes mapping to nested storage options.
    /// Dictionaries do not preserve the order of insertion, so pattern specificity is determined based on its length.
    /// </summary>
    public class PrefixedStorageOptions : Dictionary<string, StorageOptions> { }

    public class PrefixedStorage : IStorage
    {
        readonly PrefixedImpl[] _impls;

        sealed class PrefixedImpl
        {
            public readonly Glob Prefix;
            public readonly IStorage Storage;

            public PrefixedImpl(Glob prefix, IStorage storage)
            {
                Prefix  = prefix;
                Storage = storage;
            }
        }

        public PrefixedStorage(IServiceProvider services, PrefixedStorageOptions options)
        {
            var impls = new List<PrefixedImpl>(options.Count);

            foreach (var (prefix, inner) in options)
                impls.Add(new PrefixedImpl(new Glob(prefix), new GenericStorage(services, inner)));

            impls.Sort((a, b) => b.Prefix.Pattern.Length.CompareTo(a.Prefix.Pattern.Length));

            _impls = impls.ToArray();
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default)
            => Task.WhenAll(_impls.Select(s => s.Storage.InitializeAsync(cancellationToken)));

        public async Task<OneOf<StorageFile, NotFound, Exception>> ReadAsync(string name, CancellationToken cancellationToken = default)
        {
            foreach (var impl in _impls)
            {
                if (impl.Prefix.Match(name))
                    return await impl.Storage.ReadAsync(name, cancellationToken);
            }

            return new NotFound();
        }

        public async Task<OneOf<Success, Exception>> WriteAsync(StorageFile file, CancellationToken cancellationToken = default)
        {
            foreach (var impl in _impls)
            {
                if (impl.Prefix.Match(file.Name))
                    return await impl.Storage.WriteAsync(file, cancellationToken);
            }

            try
            {
                // set trace
                throw new DirectoryNotFoundException($"No prefixes matched for file '{file.Name}'.");
            }
            catch (Exception e)
            {
                return e;
            }
        }

        public Task DeleteAsync(string[] names, CancellationToken cancellationToken = default)
        {
            var batches = new Dictionary<PrefixedImpl, List<string>>(_impls.Length);

            foreach (var impl in _impls)
            foreach (var name in names)
            {
                if (impl.Prefix.Match(name))
                {
                    if (batches.TryGetValue(impl, out var list))
                        list.Add(name);
                    else
                        batches[impl] = new List<string>(names.Length) { name };
                }
            }

            return Task.WhenAll(batches.Select(x => x.Key.Storage.DeleteAsync(x.Value.ToArray(), cancellationToken)));
        }

        public void Dispose()
        {
            foreach (var impl in _impls)
                impl.Storage.Dispose();
        }
    }
}