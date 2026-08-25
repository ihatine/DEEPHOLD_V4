using UnityEngine;
using Unity.Netcode;

namespace OutOfSync.Gameplay
{
    public sealed class Inventory : NetworkBehaviour
    {
        public NetworkVariable<int> Wood = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<int> Stone = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<int> Torch = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<int> Copper = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<int> Crystal = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<int> Food = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private readonly int[] local = new int[6];
        public int SelectedSlot { get; private set; }
        public bool IsStandalone => !IsSpawned && NetworkManager.Singleton == null;
        public int WoodCount => IsStandalone ? local[0] : Wood.Value;
        public int StoneCount => IsStandalone ? local[1] : Stone.Value;
        public int TorchCount => IsStandalone ? local[2] : Torch.Value;
        public int CopperCount => IsStandalone ? local[3] : Copper.Value;
        public int CrystalCount => IsStandalone ? local[4] : Crystal.Value;
        public int FoodCount => IsStandalone ? local[5] : Food.Value;

        private void Update()
        {
            for (int i = 0; i < 8; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) SelectSlot(i);
        }

        public void SelectSlot(int slot)
        {
            SelectedSlot = Mathf.Clamp(slot, 0, 7);
            ToolSystem.SelectSlot(SelectedSlot);
        }

        [ServerRpc]
        public void AddResourceServerRpc(int kind, int amount)
        {
            if (amount <= 0) return;
            AddNetworkResource(kind, amount);
        }


        public void AddResourceAuthoritative(int kind, int amount)
        {
            if (!IsSpawned || !IsServer || amount <= 0) return;
            AddNetworkResource(kind, amount);
        }

        public void AddResourceLocal(int kind, int amount)
        {
            if (amount <= 0 || kind < 0 || kind >= local.Length) return;
            local[kind] += amount;
        }

        private void AddNetworkResource(int kind, int amount)
        {
            switch (kind)
            {
                case 0: Wood.Value += amount; break;
                case 1: Stone.Value += amount; break;
                case 2: Torch.Value += amount; break;
                case 3: Copper.Value += amount; break;
                case 4: Crystal.Value += amount; break;
                case 5: Food.Value += amount; break;
            }
        }
    }
}
