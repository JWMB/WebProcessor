using AutoBogus;
using AutoBogus.FakeItEasy;
using FakeItEasy;
using Microsoft.Extensions.FileProviders;
using Shouldly;

namespace TrainingApi.Tests
{
    public class FallbackFileProviderTests
    {
        [Theory]
        [InlineData("/teacher", "?path=%2Fteacher")]
        [InlineData("/index.html", null)]
        [InlineData("/Index.HTML", null)]
        [InlineData("/login", "?path=%2Flogin")]
        [InlineData("/teacher?group=abc", "?path=%2Fteacher%3Fgroup%3Dabc")]
        public void FallbackFileProvider_ShouldRewriteUrl(string subPath, string? expectedRewrite)
        {
            var staticPath = "/admin";
            var fallbackFile = "index.html";

            var inner = new AutoFaker<IFileProvider>()
                .Configure(config => config.WithBinder<FakeItEasyBinder>())
                .Generate();

            var fileProvider = new FallbackFileProvider("index.html", inner, staticPath);

            var requestUri = new Uri($"https://localhost{staticPath}{subPath}");

            var file = A.Fake<IFileInfo>();
            A.CallTo(() => file.Name).Returns("index.html"); //subPath.Trim('/').Split('/').Last());

            var doRewrite = fileProvider.ShouldRewriteUrl(file, requestUri, out var path);

            doRewrite.ShouldBe(expectedRewrite != null);
            if (doRewrite)
                path.ShouldBe($"{staticPath}/{fallbackFile}{expectedRewrite}");
        }
    }
}
