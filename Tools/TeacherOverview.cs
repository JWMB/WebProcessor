using ProblemSourceModule.Services.Storage;

namespace Tools
{
	internal class TeacherOverview
	{
		private readonly IUserRepository userRepository;

		public TeacherOverview(IUserRepository userRepository)
		{
			this.userRepository = userRepository;
		}
		public async Task X()
		{
			var allUsers = await userRepository.GetAll();
			var relevant = allUsers
				.Where(o => o.Trainings.GetAllIds().Count > 10)
				.GroupBy(o => string.Join(".", o.Email.Split("@").Last().Split(".").TakeLast(2)))
				.Where(o => o.Count() >= 3)
				.ToDictionary(o => o.Key, o => o.Select(p => new { p.Email, NumTrainings = p.Trainings.GetAllIds().Count }).ToList())
				.OrderByDescending(o => o.Value.Count);

			var dbg = string.Join("\n", relevant.Select(o => $"{o.Key}\n{string.Join("\n", o.Value.Select(p => $"\t{p.Email}\t{p.NumTrainings}"))}"));
			var dbg2 = string.Join("\n", relevant.Select(o => $"{o.Key}\t{o.Value.Count}"));
		}
	}
}
