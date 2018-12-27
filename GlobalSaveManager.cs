//Thanks to Omnicron for the reference to
//https://pastebin.com/YY0XKMCQ
//For BinarryFormatting any System.Serializable into a custom file

using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public static class GlobalSaveManager {

    private static GameObject Player;
    private static List<CustomSaveEvent> EventSaves = new List<CustomSaveEvent>();

    private static EventListener EventListener = new EventListener();

    public static void RegisterPlayer(GameObject player)
    {
        Player = player;
        Debug.Log("Registered Player.");
    }

    public static void RegisterEvent(CustomSaveEvent customEvent)
    {
        EventSaves.Add(customEvent);
        EventSaves = EventSaves.OrderBy(x => x.EventTime).ToList();
        Debug.Log(customEvent.ToString());
    }

    public static List<CustomSaveEvent> GetRelaventSaveData(int sceneIndex)
    {
        return EventSaves.Where(x => x.SceneIndex == sceneIndex).ToList();
    }

    public static void Save()
    {
        CustomSaveData DataToSave = new CustomSaveData() { SaveData = EventSaves, PlayerPosition = Player.transform.position, PlayerRotation = Player.transform.rotation};

        GlobalVariables globalVariables = (GlobalVariables)GameObject.Find("Global").GetComponent("GlobalVariables");

        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Open(@"C:\Users\David Blerkselmans\Desktop\PKU TestSaves\Save1.pku", FileMode.OpenOrCreate, FileAccess.Write);
        bf.Serialize(file, DataToSave);
        file.Close();
    }

    public static void Load()
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Open(@"C:\Users\David Blerkselmans\Desktop\PKU TestSaves\Save1.pku", FileMode.Open, FileAccess.Read);
        CustomSaveData DataToLoad = (CustomSaveData)bf.Deserialize(file);

        EventSaves = DataToLoad.SaveData;
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

        //public Pokemon[] Party;
        //public Bag PlayerBag;

        public List<CustomSaveEvent> SaveData;
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
