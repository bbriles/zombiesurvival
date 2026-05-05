using Godot;
using System;

public partial class Item : Area3D
{
	public enum ItemType
    {
        Grenade,
        HealthPack,
        Ammo,
    }

    [Export] public ItemType Type { get; set; }
    [Export] public int Amount { get; set; } // e.g. how many grenades, how much health, etc.

    public void OnBodyEntered(Node body)
    {
        if (body is Player player)
        {
            player.PickupItem(this);
            QueueFree();
        }
    }
}
