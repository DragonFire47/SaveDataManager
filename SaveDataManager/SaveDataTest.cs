using System.IO;
using System.Text;

namespace SaveDataManager
{
    class SaveDataTest : PMLSaveData
    {
        public override string Identifier()
        {
            return "Test";
        }

        public override void LoadData(MemoryStream dataStream)
        {
            using (BinaryReader reader = new BinaryReader(dataStream))
            {
                string value = reader.ReadString();
                PulsarModLoader.Utilities.Logger.Info("read: " + value);
            }
        }

        public override MemoryStream SaveData()
        {
            MemoryStream stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.Default, true))
            {
                writer.Write("Codeword");
            }
            return stream;
        }
    }
}
