using Microsoft.Extensions.FileProviders;

namespace TrainingApi
{
    public class FallbackFileProvider : IFileProvider
    {
        private readonly string fallbackFile;
        private IFileProvider inner;
        public FallbackFileProvider(string fallbackFile, IFileProvider inner)
        {
            this.fallbackFile = fallbackFile;
            this.inner = inner;
        }

        public IDirectoryContents GetDirectoryContents(string subpath) => inner.GetDirectoryContents(subpath);
        public Microsoft.Extensions.Primitives.IChangeToken Watch(string filter) => inner.Watch(filter);

        public IFileInfo GetFileInfo(string subpath)
        {
            var file = inner.GetFileInfo(subpath);
            return file.Exists ? file : inner.GetFileInfo(fallbackFile);
        }
    }
}
