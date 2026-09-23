using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemSourceModule.Models
{
	public class AiAnalysis
	{
		public required DocumentReference Source { get; set; }
		public DateTime Created { get; set; }

		public required string Type { get; set; }
		public string? Prompt { get; set; }
		public string? Analysis { get; set; }
	}

	public class DocumentReference
	{
		public required string Cls { get; set; }
		public required string Id { get; set; }
		public string? Name { get; set; }
	}
}
