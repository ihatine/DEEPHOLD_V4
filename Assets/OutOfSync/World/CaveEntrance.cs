using UnityEngine;

namespace OutOfSync.World
{
    public sealed class CaveEntrance : MonoBehaviour
    {
        public bool IsExit { get; private set; }

        public void Configure(bool exit) => IsExit = exit;

        public void Use(OutOfSync.Gameplay.CoopPlayer player)
        {
            if (player == null) return;
            CaveSystem.Instance?.ToggleCave(player);
        }
    }
}
