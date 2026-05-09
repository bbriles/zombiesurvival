using Godot;
using System;

public partial class Hud : Control
{
	[Export] public Label GrenadeCountLabel;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GameManager.Player.ItemPickup += OnPlayerItemPickup;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void OnPlayerItemPickup()
	{
		if(GrenadeCountLabel != null)
		{
			GrenadeCountLabel.Text = $"{GameManager.Player.GrenadeCount}";
		}
	}
}
