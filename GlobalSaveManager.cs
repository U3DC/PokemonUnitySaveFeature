//Thanks to Omnicron for the reference to
// https://pastebin.com/YY0XKMCQ
//For BinarryFormatting any System.Serializable into a custom file

using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;
using System;

public static class GlobalSaveManager {
    private const string BuildVersion = "0.0.1";
    private static string saveLocation = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData) + @"\Pokemon Unity\Saves\";

    private static GameObject Player;
    private static List<CustomSaveEvent> EventSaves = new List<CustomSaveEvent>();

    private static EventListener EventListener = new EventListener();

    public static void RegisterPlayer(GameObject player)
    {
        Player = player;
        Debug.Log("Registered Player.");
    }

    /// <summary>
    /// Registers an event into the EventSaves List<>
    /// </summary>
    /// <param name="customEvent">The event that needs to be registered</param>
    public static void RegisterEvent(CustomSaveEvent customEvent)
    {
        EventSaves.Add(customEvent);
        EventSaves = EventSaves.OrderBy(x => x.EventTime).ToList();
        Debug.Log(customEvent.ToString());
    }

    /// <summary>
    /// Get's the relevant CustomSaveEvents for the current Scene.
    /// </summary>
    /// <param name="sceneIndex">The scene index that the Player is currently on.</param>
    /// <returns></returns>
    public static List<CustomSaveEvent> GetRelaventSaveData(int sceneIndex)
    {
        return EventSaves.Where(x => x.SceneIndex == sceneIndex).ToList();
    }

    public static void Save()
    {
        GlobalVariables globalVariables = (GlobalVariables)GameObject.Find("Global").GetComponent("GlobalVariables");

        Pokemon[][] Party = SaveData.currentSave.PC.boxes;
        Bag PlayerBag = SaveData.currentSave.Bag;

        CustomSaveData DataToSave = new CustomSaveData(Player.transform.position, Player.transform.rotation, SceneManager.GetActiveScene().buildIndex, Party, PlayerBag, EventSaves, "");
        BinaryFormatter bf = new BinaryFormatter();
        try
        {
            int saveAmount = Directory.GetFiles(saveLocation, "*pku", SearchOption.TopDirectoryOnly).Length;

            if (saveAmount < 0)
                saveAmount = 0;

            FileStream file = File.Open(saveLocation + @"Save" + saveAmount.ToString() + ".pku", FileMode.OpenOrCreate, FileAccess.Write);
            bf.Serialize(file, DataToSave);
            file.Close();
            Debug.Log("Save file created.");
        }
        catch(Exception)
        {
            Debug.Log("Pokemon Unity save directory does not exist, creating new one...");
            Directory.CreateDirectory(saveLocation.Substring(0, saveLocation.Length -1));
            Debug.Log("Trying to save again...");
            FileStream file = File.Open(saveLocation + @"Save" + (Directory.GetFiles(saveLocation).Length - 2).ToString() + ".pku", FileMode.OpenOrCreate, FileAccess.Write);
            bf.Serialize(file, DataToSave);
            file.Close();
            Debug.Log("Save file created.");
        }
    }

    public static void Load(int saveIndex)
    {
        BinaryFormatter bf = new BinaryFormatter();
        try
        {
            FileStream file = File.Open(saveLocation + "Save" + saveIndex.ToString() + ".pku", FileMode.Open, FileAccess.Read);
            CustomSaveData DataToLoad = (CustomSaveData)bf.Deserialize(file);

            if (null != DataToLoad)
            {
                //EventSaves contains all the Events that the Player has encountered
                EventSaves = DataToLoad.SaveData;

                if (SaveData.currentSave == null)
                {
                    SaveData.currentSave = new SaveData(-1);
                }

                //Loads the Trainer's Party into the CurrentSave
                SaveData.currentSave.PC.boxes = DataToLoad.Party;
                //Loads the Bag (containing the Items that the player owns) into the CurrentSave
                SaveData.currentSave.Bag = DataToLoad.PlayerBag;

                GameObject Player = GameObject.FindGameObjectWithTag("Player");
                Player.transform.position = DataToLoad.PlayerPosition;
                Player.transform.rotation = DataToLoad.PlayerRotation;

                EventSaves = EventSaves.OrderBy(x => x.EventTime).ToList();
            }

            file.Dispose();
        }
        catch(Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    static List<CustomSaveData> GetSaves(int Amount)
    {
        List<CustomSaveData> saveFiles = new List<CustomSaveData>();
        foreach (string file in Directory.GetFiles(saveLocation))
        {
            BinaryFormatter bf = new BinaryFormatter();
            try
            {
                if (Path.GetExtension(file) == "pku")
                {
                    using (FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        try
                        {
                            CustomSaveData saveData = (CustomSaveData)bf.Deserialize(fileStream);
                            if (saveData.BuildVersion == BuildVersion)
                            {
                                saveFiles.Add(saveData);
                            }
                            else
                            {
                                //Try to convert the created file into the current version
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.Log(e.ToString());
                        }
                    }
                }
            }
            catch (IOException e)
            {
                throw e;
            }
        }

        saveFiles = saveFiles.OrderBy(x => x.TimeCreated).ToList();
        if (Amount == 0 || saveFiles.Count < Amount)
        {
            return saveFiles;
        }
        else
        {
            return saveFiles.Take(Amount).ToList();
        }
    }

    [System.Serializable]
    private class CustomSaveData
    {
        public string BuildVersion = GlobalSaveManager.BuildVersion;
        public string SaveName = string.Empty;
        public DateTime TimeCreated;

        public SerializableVector3 PlayerPosition;
        public SerializableQuaternion PlayerRotation;

        public int ActiveScene;

        /// <summary>
        /// When the Player blacks out they need to return to the last visited Pokemon Center
        /// </summary>
        private int pCenterScene;
        private SerializableVector3 pCenterPosition;
        private SerializableQuaternion pCenterRotation;

        public Pokemon[][] Party;
        public Bag PlayerBag;

        public List<CustomSaveEvent> SaveData;

        public void AddPokemonCenter(int scene, SerializableVector3 position, SerializableQuaternion rotation)
        {
            pCenterScene = scene;
            pCenterPosition = position;
            pCenterRotation = rotation;
        }

        public CustomSaveData(
            SerializableVector3 playerPosition,
            SerializableQuaternion playerRotation, int activeScene,
            Pokemon[][] party, Bag playerBag, List<CustomSaveEvent> saveData,
            string saveName)
        {
            PlayerPosition = playerPosition;
            PlayerRotation = playerRotation;

            ActiveScene = activeScene;

            Party = party;
            PlayerBag = playerBag;

            SaveData = saveData;
            SaveName = saveName;

            TimeCreated = DateTime.Now;
        }
    }
}

[System.Serializable]
public enum SaveEventType
{
    ITEM,
    BATTLE,
    INTERACTION,
    UNKNOWN
}
