namespace NoK
{
    public static class UriExtensions
    {
        public static Uri WithPath(this Uri uri, string path)
        {
            return new Uri($"{uri.GetSchemeAndHost()}/{path}");
        }
        public static Uri AppendPath(this Uri uri, string path)
        {
            var combined = $"{uri.AbsolutePath}/{path}".Replace("//", "/");
            return new Uri($"{uri.GetSchemeAndHost()}{combined}{uri.Query}");
        }

        public static string GetSchemeAndHost(this Uri uri)
        {
            var port = (uri.Scheme.ToLower() == "https" && uri.Port != 443)
                || (uri.Scheme.ToLower() == "http" && uri.Port != 80)
                ? $":{uri.Port}"
                : "";
            return $"{uri.Scheme.ToLower()}://{uri.DnsSafeHost}{port}";
        }
    }
}
