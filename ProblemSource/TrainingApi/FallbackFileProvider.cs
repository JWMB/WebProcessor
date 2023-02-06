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

        public bool ShouldRewriteUrl(IFileInfo file, Uri uri, out string rewritten)
        {
            // Called upon OnPrepareResponse
            
            // If file is the fallback file, it's either b/c it was actually requested, or because of the fallback
            // TODO: we currently don't care about fallbackFile path - e.g. /sub/index.html and /index.html are treated the same now

            var path = uri.AbsolutePath.ToString().ToLower();
            if (file.Name != fallbackFile || !path.StartsWith(RootPath))
            {
                // Was not the fallback file, so we can serve it directly
                rewritten = uri.AbsolutePath.ToString();
                return false;
            }

            if (file.Name == fallbackFile && path.EndsWith(fallbackFile)) // request.QueryString.HasValue && request.QueryString.ToString().StartsWith(qp))
            {
                // the fallback file was specifically requested
                rewritten = uri.AbsolutePath.ToString();
                return false;
            }

            var subPathAndQuery = uri.PathAndQuery.Substring(RootPath.Length); //path.Substring(RootPath.Length);
            rewritten = $"{RootPath}/{fallbackFile}?path={Uri.EscapeDataString(subPathAndQuery)}";
            return true;
        }

        public static string SplitJoin(string str, char split, Func<IEnumerable<string>, IEnumerable<string>> func)
        {
            return string.Join(split, func(str.Split(split)));
        }
    }
}
