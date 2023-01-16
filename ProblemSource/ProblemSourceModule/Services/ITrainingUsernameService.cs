using ProblemSource;

namespace ProblemSourceModule.Services
{
    public interface ITrainingUsernameService
    {
        string FromId(int id);
    }

    public class MnemoJapaneseTrainingUsernameService : ITrainingUsernameService
    {
        private readonly MnemoJapanese mnemoJapanese;
        private readonly UsernameHashing usernameHashing;

        public MnemoJapaneseTrainingUsernameService(MnemoJapanese mnemoJapanese, UsernameHashing usernameHashing)
        {
            this.mnemoJapanese = mnemoJapanese;
            this.usernameHashing = usernameHashing;
        }

        public string FromId(int id)
        {
            return usernameHashing.Hash(mnemoJapanese.FromIntWithRandom(id));
        }
    }

}
