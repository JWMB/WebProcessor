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
            var file = inner.GetFileInfo(subpath);
            return file.Exists ? file : inner.GetFileInfo(fallbackFile);
        }

        public bool ShouldRewriteUrl(IFileInfo file, HttpRequest request, out string rewritten)
        {
            // TODO: we currently don't care about fallbackFile path - e.g. /sub/index.html and /index.html are treated the same now
            var path = request.Path.ToString().ToLower();
            if (file.Name != fallbackFile || !path.StartsWith(RootPath))
            {
                rewritten = request.Path.ToString();
                return false;
            }
            var qp = "?path=";
            if (file.Name == fallbackFile && path.EndsWith(fallbackFile)) // request.QueryString.HasValue && request.QueryString.ToString().StartsWith(qp))
            {
                rewritten = request.Path.ToString();
                return false;
            }
            var subPath = path.Substring(RootPath.Length);
            rewritten = $"{RootPath}/{fallbackFile}{qp}{Uri.EscapeDataString(subPath)}";
            return true;
        }

        public static string SplitJoin(string str, char split, Func<IEnumerable<string>, IEnumerable<string>> func)
        {
            return string.Join(split, func(str.Split(split)));
        }
    }
}
