using System.IO;
using PulsarModLoader;

namespace SaveDataManager
{
    public abstract class PMLSaveData
    {
        public PulsarMod MyMod;
        public abstract string Identifier();
        public virtual uint VersionID => 0;
        public abstract MemoryStream SaveData();
        public abstract void LoadData(MemoryStream dataStream, uint VersionID);
    }
}
