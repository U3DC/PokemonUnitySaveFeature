//Thanks to Omnicron for the reference to
// https://pastebin.com/YY0XKMCQ
//For BinarryFormatting any System.Serializable into a custom file

using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;

public static class GlobalSaveManager {

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

        CustomSaveData DataToSave = new CustomSaveData(Player.transform.position, Player.transform.rotation, SceneManager.GetActiveScene().buildIndex, Party, PlayerBag, EventSaves);

        string AppData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Open(AppData + @"\Pokemon Unity\Saves\Save" + (Directory.GetFiles(AppData).Length - 1).ToString() + ".pku", FileMode.OpenOrCreate, FileAccess.Write);

        bf.Serialize(file, DataToSave);
        file.Close();
    }

    public static void Load()
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Open(@"C:\Users\David Blerkselmans\Desktop\PKU TestSaves\Save1.pku", FileMode.Open, FileAccess.Read);

        CustomSaveData DataToLoad = (CustomSaveData)bf.Deserialize(file);

        //EventSaves contains all the Events that the Player has encountered
        EventSaves = DataToLoad.SaveData;

        //Loads the Trainer's Party into the CurrentSave
        SaveData.currentSave.PC.boxes = DataToLoad.Party;
        //Loads the Bag (containing the Items that the player owns) into the CurrentSave
        SaveData.currentSave.Bag = DataToLoad.PlayerBag;

        GameObject Player = GameObject.FindGameObjectWithTag("Player");
        Player.transform.position = DataToLoad.PlayerPosition;
        Player.transform.rotation = DataToLoad.PlayerRotation;

        EventSaves = EventSaves.OrderBy(x => x.EventTime).ToList();
    }

    [System.Serializable]
    private class CustomSaveData
    {
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
            Pokemon[][] party, Bag playerBag, List<CustomSaveEvent> saveData)
        {
            PlayerPosition = playerPosition;
            PlayerRotation = playerRotation;

            ActiveScene = activeScene;

            Party = party;
            PlayerBag = playerBag;

            SaveData = saveData;
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
