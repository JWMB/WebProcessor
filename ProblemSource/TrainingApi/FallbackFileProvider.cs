using Microsoft.Extensions.FileProviders;

namespace TrainingApi
{
    public class FallbackFileProvider : IFileProvider
    {
        private readonly string fallbackFile;
        private readonly IFileProvider inner;

        public string RootPath { get; }

        public FallbackFileProvider(string fallbackFile, IFileProvider inner, string rootPath)
        {
            this.fallbackFile = fallbackFile.ToLower();
            this.inner = inner;
            RootPath = rootPath;
        }

        public IDirectoryContents GetDirectoryContents(string subpath) => inner.GetDirectoryContents(subpath);
        public Microsoft.Extensions.Primitives.IChangeToken Watch(string filter) => inner.Watch(filter);

        public IFileInfo GetFileInfo(string subpath)
        {
            // First, this method is called by UseStaticFiles middleware
            // If the file exists, we return it - otherwise we return the fallback file
            var file = inner.GetFileInfo(subpath);
            return file.Exists ? file : inner.GetFileInfo(fallbackFile);
        }
    }
}
