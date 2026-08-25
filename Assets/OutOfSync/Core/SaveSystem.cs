using UnityEngine;
using System.IO;

namespace OutOfSync.Core
{
    public static class SaveSystem
    {
        [System.Serializable]
        private class SaveData
        {
            public float x;
            public float y;
            public int wood;
            public int stone;
            public int torch;
        }

        private static string PathName => Path.Combine(Application.persistentDataPath, "deephold_save.json");
        public static bool HasSave() => File.Exists(PathName);

        public static void Save(Gameplay.CoopPlayer player)
        {
            if (player == null) return;
            var inv = player.GetComponent<Gameplay.Inventory>();
            var data = new SaveData
            {
                x = player.transform.position.x,
                y = player.transform.position.y,
                wood = inv?.WoodCount ?? 0,
                stone = inv?.StoneCount ?? 0,
                torch = inv?.TorchCount ?? 0
            };
            File.WriteAllText(PathName, JsonUtility.ToJson(data, true));
        }
    }
}
