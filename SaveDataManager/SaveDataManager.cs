using HarmonyLib;
using PulsarModLoader;
using PulsarModLoader.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SaveDataManager
{
    class SaveDataManager
    {
        public SaveDataManager()
        {
            foreach(PulsarMod mod in ModManager.Instance.GetAllMods())
            {
                OnModLoaded(mod.Name, mod);
            }
            ModManager.Instance.OnModSuccessfullyLoaded += OnModLoaded;
            ModManager.Instance.OnModUnloaded += OnModRemoved;
            Instance = this;
        }
        public static SaveDataManager Instance;

        static public string SaveDir = Directory.GetCurrentDirectory() + "/Saves";
        static public string LocalSaveDir = SaveDir + "/Local";

        List<PMLSaveData> SaveConfigs = new List<PMLSaveData>();

        void OnModLoaded(string modName, PulsarMod mod)
        {
            mod.GetType().Assembly.GetTypes().AsParallel().ForAll((type) =>
            {
                if ( typeof(PMLSaveData).IsAssignableFrom(type) )
                {
                    PMLSaveData SaveData = (PMLSaveData)Activator.CreateInstance(type);
                    SaveData.MyMod = mod;
                    SaveConfigs.Add( SaveData );
                }
            });
        }

        void OnModRemoved(PulsarMod mod)
        {
            List<PMLSaveData> saveConfigsToRemove = new List<PMLSaveData>();
            SaveConfigs.AsParallel().ForAll((arg) => 
            {
                if (arg.GetType().Assembly == mod.GetType().Assembly)
                {
                    saveConfigsToRemove.Add(arg);
                }
            });
            for (byte s = 0; s < saveConfigsToRemove.Count; s++)
            {
                SaveConfigs.Remove(saveConfigsToRemove[s]);
            }
        }

        public static string getPMLSaveFileName(string inFileName)
        {
            return inFileName.Replace(".plsave", ".pmlsave");
        }

        public void SaveDatas(string inFileName)
        {
            //Start Saving, create temp file
            string fileName = getPMLSaveFileName(inFileName);
            string tempText = fileName + "_temp";
            FileStream fileStream = File.Create(tempText);
            BinaryWriter binaryWriter = new BinaryWriter(fileStream);

            //save for mods
            binaryWriter.Write(SaveConfigs.Count);
            foreach(PMLSaveData saveData in SaveConfigs)
            {
                MemoryStream dataStream = saveData.SaveData();          //Collect Save data from mod
                binaryWriter.Write(saveData.MyMod.HarmonyIdentifier()); //Write Mod Identifier
                binaryWriter.Write(saveData.Identifier());              //Write PMLSaveData Identifier
                binaryWriter.Write((int)dataStream.Length);             //Write Stream Byte count
                dataStream.CopyTo(binaryWriter.BaseStream);             //Copy save data to file
            }

            //Finish Saving, close and save file to actual location
            binaryWriter.Close();
            fileStream.Close();
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            File.Move(tempText, inFileName);
            string relativeFileName = PLNetworkManager.Instance.FileNameToRelative(fileName);
            Logger.Info("PMLSaveManager has saved file: " + relativeFileName);
        }

        public void LoadDatas(string inFileName)
        {
            //start reading
            string fileName = getPMLSaveFileName(inFileName);
            FileStream fileStream = File.OpenRead(fileName);
            BinaryReader binaryReader = new BinaryReader(fileStream);

            //read for mods
            int count = binaryReader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                string harmonyIdent = binaryReader.ReadString(); //HarmonyIdentifier
                string SavDatIdent = binaryReader.ReadString();  //SaveDataIdentifier
                int bytecount = binaryReader.ReadInt32();       //ByteCount
                foreach(PMLSaveData savedata in SaveConfigs)
                {
                    if(savedata.MyMod.HarmonyIdentifier() == harmonyIdent && savedata.Identifier() == SavDatIdent)
                    {
                        MemoryStream stream = new MemoryStream();               //initialize new memStream
                        binaryReader.BaseStream.CopyTo(stream, bytecount);      //Copy file data to memStream
                        savedata.LoadData(stream);                              //Send memStream to PMLSaveData
                    }
                }
            }

            //Finish Reading
            binaryReader.Close();
            fileStream.Close();
            Logger.Info("PMLSaveManager has read file: " + PLNetworkManager.Instance.FileNameToRelative(fileName));
        }
    }
    [HarmonyPatch(typeof(PLSaveGameIO), "SaveToFile")]
    class SavePatch
    {
        static void Postfix(string inFileName)
        {
            SaveDataManager.Instance.SaveDatas(inFileName);
        }
    }
    [HarmonyPatch(typeof(PLSaveGameIO), "LoadFromFile")]
    class LoadPatch
    {
        static void Postfix(string inFileName)
        {
            SaveDataManager.Instance.LoadDatas(inFileName);
        }
    }
    [HarmonyPatch(typeof(PLSaveGameIO), "DeleteSaveGame")]
    class DeletePatch
    {
        static void Prefix(PLSaveGameIO __instance)
        {
            string fileName = SaveDataManager.getPMLSaveFileName(__instance.LatestSaveGameFileName);
            if (fileName != "")
            {
                try
                {
                    Logger.Info("DeleteSaveGame  " + fileName);
                    File.Delete(__instance.LatestSaveGameFileName);
                }
                catch (Exception ex)
                {
                    Logger.Info("DeleteSaveGame EXCEPTION: " + ex.Message + ": Could not delete save file!");
                }
            }
        }
    }
}
