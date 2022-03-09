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

        public override void LoadData(MemoryStream filestream)
        {
            using (BinaryReader reader = new BinaryReader(filestream))
            {
                string value = reader.ReadString();
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
