using PulsarModLoader;

namespace SaveDataManager
{
    public class Mod : PulsarMod
    {
        public Mod()
        {
            new SaveDataManager();
        }

        public override string Version =>"0.0.1";

        public override string Author => "Dragon";

        public override string Name => "SaveDataManager";

        public override string HarmonyIdentifier()
        {
            return $"{Author}.{Name}";
        }
    }
}
