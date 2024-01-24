using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoK.Tests
{
    public class Class1
    {
        [Fact]
        public async Task X()
        {
            IStimuliRepository problemRepository = new NoKProblemRepository(new NoKProblemRepository.Config(""));

            var stimulus = await problemRepository.GetById("");
            stimulus.ShouldNotBeNull();

            var checker = new NoKSolutionChecker((NoKProblemRepository)problemRepository);
            await checker.Check(stimulus, new SimpleUserResponse());
        }
    }
}
