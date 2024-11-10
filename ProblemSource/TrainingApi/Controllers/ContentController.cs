using Microsoft.AspNetCore.Mvc;
using NoK;
using NoK.Models;

namespace TrainingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContentController : ControllerBase
    {
        private readonly NoKStimuliRepository stimuliRepository;

        public ContentController(NoKStimuliRepository stimuliRepository)
        {
            this.stimuliRepository = stimuliRepository;
        }

        [HttpGet("/{id}")]
        public Task<TreeNodeDto?> GetSingle(string id, bool includeSlimChildren)
        {
            var found = stimuliRepository.ContentNodes.FirstOrDefault(o => o.Id.ToString() == id);
            return Task.FromResult(
                found == null
                ? (TreeNodeDto?)null
                : CreateFromContentNode(found, includeSlimChildren ? found.Children().Select(o => CreateFromContentNode(o, null, false)) : null, true));
        }

        [HttpGet("tree/{id?}")]
        public Task<TreeNodeDto> GetTree(int? id = null, bool includeDescendantBodies = false)
        {
            var roots = id == null
                ? stimuliRepository.ContentNodes.Where(o => o.Parent == null)
                : stimuliRepository.ContentNodes.Where(o => o.Id == id);
            var resultRoot = roots.Count() > 1
                ? new TreeNodeDto("root", "", "", roots.Select(o => Rec(o, -1)).ToList())
                : Rec(roots.First(), 0);

            return Task.FromResult(resultRoot);

            TreeNodeDto Rec(ContentNode parent, int depth) => 
                CreateFromContentNode(parent, parent.Children().Select(o => Rec(o, depth + 1)), depth < 1 ? true : includeDescendantBodies);
        }

        private TreeNodeDto CreateFromContentNode(ContentNode item, IEnumerable<TreeNodeDto>? children = null, bool includeDescendantBodies = true) =>
            new TreeNodeDto(item.Name, item.Id.ToString(), item.GetType().Name, children?.ToList() ?? new(), includeDescendantBodies ? item.OtherContent : null);

        public readonly record struct TreeNodeDto(
            string Title,
            string Id,
            string Type,
            List<TreeNodeDto> Children,
            string? Body = null,
            string? Icon = null
            );
    }
}