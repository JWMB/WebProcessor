using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using System.Web;
using TrainingApi.Authorization;

namespace TrainingApi.Controllers
{
	[AtLeastRole(Roless.Admin)]
	[ApiController]
	[Route("api/[controller]")]
	public class FilesController : ControllerBase
	{
		private readonly IFileProvider fileProvider;

		public FilesController(IFileProvider fileProvider)
		{
			this.fileProvider = fileProvider;
		}

		[HttpGet("list")]
		public async Task<List<FileEntry>> List(string subPath = "")
		{
			var contents = fileProvider.GetDirectoryContents(subPath);
			var uri = new Uri(Request.GetDisplayUrl());
			return contents.Select(o => new FileEntry(o.Name, o.Length, o.IsDirectory, $"{SchemeAndHostWithPort(uri)}/api/Files?subpath={HttpUtility.UrlEncode($"{subPath}/{o.Name}")}"))
				.ToList();
		}
		public record FileEntry(string Name, long Length, bool IsDirectory, string DownloadPath);

		private static string SchemeAndHostWithPort(Uri uri)
			=> $"{uri.Scheme}://{uri.Host}{(uri.Port == 80 ? "" : $":{uri.Port}")}";

		[HttpGet()]
		public async Task<IActionResult> Get(string subPath)
		{
			var fileInfo = fileProvider.GetFileInfo(subPath);
			if (!fileInfo.Exists || fileInfo.PhysicalPath == null)
				return NotFound();

			var typeProvider = new FileExtensionContentTypeProvider();
			var contentType = typeProvider.TryGetContentType(subPath, out var v) ? v : "application/octet-stream";

			return PhysicalFile(fileInfo.PhysicalPath, contentType, fileInfo.Name);
		}

	}
}
