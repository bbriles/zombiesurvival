using Godot;
using System;

public partial class Hud : Control
{
	[Export] public Label GrenadeCountLabel;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GameManager.Player.ItemPickup += OnPlayerItemPickup;
		GameManager.Player.ItemUsed += OnPlayerItemUsed; // Update grenade count on use as well
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void OnPlayerItemPickup()
	{
		RefreshValues();
	}

	private void OnPlayerItemUsed()
	{
		RefreshValues();
	}

	private void RefreshValues()
	{
		if(GrenadeCountLabel != null)
		{
			GrenadeCountLabel.Text = $"{GameManager.Player.GrenadeCount}";
		}
	}
}
