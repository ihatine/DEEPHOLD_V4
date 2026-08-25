using UnityEngine;
using OutOfSync.World;

namespace OutOfSync.Gameplay
{
    public enum ToolType
    {
        Hand,
        WoodenAxe,
        WoodenPickaxe,
        WoodenSword
    }

    public static class ToolSystem
    {
        public static ToolType SelectedTool { get; private set; } = ToolType.WoodenAxe;

        public static readonly string[] SlotNames =
        {
            "WOOD AXE", "WOOD PICK", "WOOD SWORD", "TORCH",
            "WOOD", "STONE", "COPPER", "CRYSTAL"
        };

        public static void SelectSlot(int slot)
        {
            SelectedTool = slot switch
            {
                0 => ToolType.WoodenAxe,
                1 => ToolType.WoodenPickaxe,
                2 => ToolType.WoodenSword,
                _ => SelectedTool
            };
        }

        public static bool CanBreak(ResourceToolRequirement requirement)
        {
            return requirement switch
            {
                ResourceToolRequirement.Axe => SelectedTool == ToolType.WoodenAxe,
                ResourceToolRequirement.Pickaxe => SelectedTool == ToolType.WoodenPickaxe,
                ResourceToolRequirement.Sword => SelectedTool == ToolType.WoodenSword,
                ResourceToolRequirement.None => true,
                _ => false
            };
        }

        public static float DamagePerSecond(ResourceToolRequirement requirement)
        {
            if (!CanBreak(requirement)) return 0f;
            return requirement switch
            {
                ResourceToolRequirement.Axe => 1.0f,
                ResourceToolRequirement.Pickaxe => 1.0f,
                _ => 1.0f
            };
        }

        public static string DisplayName => SelectedTool switch
        {
            ToolType.WoodenAxe => "ДЕРЕВЯННЫЙ ТОПОР",
            ToolType.WoodenPickaxe => "ДЕРЕВЯННАЯ КИРКА",
            ToolType.WoodenSword => "ДЕРЕВЯННЫЙ МЕЧ",
            _ => "РУКА"
        };
    }
}
