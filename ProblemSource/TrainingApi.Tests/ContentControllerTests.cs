using Shouldly;
using TrainingApi.Controllers;

namespace TrainingApi.Tests
{
    public class ContentControllerTests : IAsyncLifetime
    {
        private ContentController? sut;

        public async Task InitializeAsync()
        {
            var directory = new DirectoryInfo(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "NoK"));
            var repo = new NoK.NoKStimuliRepository(new(directory.FullName));
            await repo.Init();
            sut = new ContentController(repo);
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task GetSingle()
        {
            var result = await sut!.GetSingle("2734");
            result.ShouldNotBeNull();
            result.Value.Children.Count.ShouldBe(1);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetTree(bool includeDescendantBodies)
        {
            var root = await sut!.GetTree(null, includeDescendantBodies);

            var flat = GetFlat(root);

            // TODO: flat.Count().ShouldBe(repo.ContentNodes.Count()); should be 524 but was 544

            var numWithBody = flat.Count(o => o.Body?.Length > 0);
            if (includeDescendantBodies)
                numWithBody.ShouldBeGreaterThan(1);
            else
                numWithBody.ShouldBe(1);

            IEnumerable<ContentController.TreeNodeDto> GetFlat(ContentController.TreeNodeDto parent)
            {
                yield return parent;
                foreach (var node in parent.Children)
                    foreach (var item in GetFlat(node))
                        yield return item;
            }
        }
    }
}
