//
//This file is excluded in the .csproj file.
//
//This was my attempt at saving the modded files to steam. I kept getting invalid handle responses when attempting to read.
//
using HarmonyLib;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Logger = PulsarModLoader.Utilities.Logger;

namespace SaveDataManager
{
    [HarmonyPatch(typeof(PLNetworkManager), "SteamCloud_SyncFileName_KeepLatest")]
    class StartSteamDLManagePatch
    {
        static void Postfix()
        {
            if (!SteamDLManagePatch.ShouldStartDownloading)
            {
                SteamDLManagePatch.ShouldStartDownloading = true;
            }
        }
    }

    [HarmonyPatch(typeof(PLNetworkManager), "SteamCloud_SyncFileName_KeepLatest")]
    class StartSteamDLManagePatch
    {
        static void Postfix()
        {
            if (!SteamDLManagePatch.ShouldStartDownloading)
            {
                SteamDLManagePatch.ShouldStartDownloading = true;
            }
        }
    }

    public void OnRemoteFileWriteAsyncComplete(RemoteStorageFileWriteAsyncComplete_t pCallback, bool bIOFailure, string inFileName)
    {
        PLNetworkManager.SyncTimestampOnLocalFileToRemote(inFileName);
        Logger.Info("OnRemoteFileWriteAsyncComplete: " + pCallback.m_eResult.ToString());
    }

    public void OnRemoteFileReadAsyncComplete(RemoteStorageFileReadAsyncComplete_t pCallback, bool bIOFailure, string inFileName)
    {
        Logger.Info("OnRemoteFileReadAsyncComplete: " + pCallback.m_eResult.ToString() + "      m_cubRead: " + pCallback.m_cubRead.ToString());
        if (bIOFailure)
        {
            Logger.Info("OnRemoteFileReadAsyncComplete:        bIOFailure: " + bIOFailure.ToString());
            return;
        }
        if (pCallback.m_eResult == EResult.k_EResultOK)
        {
            byte[] array = new byte[pCallback.m_cubRead];
            bool flag = SteamRemoteStorage.FileReadAsyncComplete(pCallback.m_hFileReadAsync, array, pCallback.m_cubRead);
            if (flag)
            {
                File.WriteAllBytes(inFileName, array);
                PLNetworkManager.SyncTimestampOnLocalFileToRemote(inFileName);
                PLSaveGameIO.Instance.UpdateFromSaveDir();
            }
            Logger.Info("FileReadAsyncComplete called:    result:" + flag.ToString());
        }
    }

    [HarmonyPatch(typeof(PLSaveGameIO), "Start")]
    class SteamDLManagePatch
    {
        public static bool ShouldStartDownloading = false;
        static void Postfix(PLSaveGameIO __instance)
        {
            SteamDLManagePatch thing = new SteamDLManagePatch();
            __instance.StartCoroutine(thing.ManagePMLSteamCloud_Init());
        }
        public bool SteamCloud_ReadFileName(string inFileName, CallResult<RemoteStorageFileReadAsyncComplete_t>.APIDispatchDelegate inDelegate)
        {
            if (!PLNetworkManager.Instance.IsLoggedIntoSteam_And_SteamworksIsSetup())
            {
                return false;
            }
            if (!SteamRemoteStorage.IsCloudEnabledForAccount())
            {
                return false;
            }
            if (!SteamRemoteStorage.IsCloudEnabledForApp())
            {
                return false;
            }
            inFileName = PLNetworkManager.Instance.FileNameToRelative(inFileName);
            uint fileSize = (uint)SteamRemoteStorage.GetFileSize(inFileName);
            Logger.Info("Attempting to download: " + inFileName);
            Logger.Info("With Filesize: " + fileSize.ToString());
            SteamAPICall_t steamAPICall_t = SteamRemoteStorage.FileReadAsync(inFileName, 0U, fileSize);
            Debug.Log("Calling FileReadAsync");
            if (steamAPICall_t != SteamAPICall_t.Invalid)
            {
                Debug.Log("Calling FileReadAsync: success");
                //PLNetworkManager.Instance.OnFileReadComplete.Set(steamAPICall_t, inDelegate);
                return true;
            }
            Debug.Log("Calling FileReadAsync: invalid handle returned!");
            return false;
        }
        public IEnumerator ManagePMLSteamCloud_Init()
        {
            int endOfFrame = 0;
            WaitForSeconds endForShortDelay = new WaitForSeconds(2f);
            List<string> SaveGamesAlreadySynced = new List<string>();
            while (Application.isPlaying)
            {
                while (!ShouldStartDownloading)
                {
                    yield return endForShortDelay;
                }
                if (PLNetworkManager.Instance.IsLoggedIntoSteam_And_SteamworksIsSetup())
                {
                    int fileCount = SteamRemoteStorage.GetFileCount();
                    List<string> steamCloudFiles = new List<string>();
                    for (int j = 0; j < fileCount; j++)
                    {
                        int num = 0;
                        steamCloudFiles.Add(SteamRemoteStorage.GetFileNameAndSize(j, out num));
                    }
                    int num2;
                    for (int i = 0; i < steamCloudFiles.Count; i = num2 + 1)
                    {
                        string name = steamCloudFiles[i];
                        if (SteamRemoteStorage.FilePersisted(name) && name.EndsWith(".pmlsave") && !File.Exists(name))
                        {
                            if (SteamCloud_ReadFileName(name, delegate (RemoteStorageFileReadAsyncComplete_t pCallback, bool bIOFailure)
                            {
                                SaveDataManager.Instance.OnRemoteFileReadAsyncComplete(pCallback, bIOFailure, name);
                            }))
                            {
                                yield return endForShortDelay;
                            }
                            else
                            {
                                yield return endOfFrame;
                            }
                        }
                        num2 = i;
                    }
                    yield return endForShortDelay;
                }
                yield return endForShortDelay;
            }
            yield break;
        }
    }
}
