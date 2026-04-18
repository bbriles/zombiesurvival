using Godot;
using System;

public partial class Level : Node3D
{
	[Export] public PauseScreen PauseScreen;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GameManager.CurrentScenePath = GetSceneFilePath();
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("ui_cancel"))
		{
			PauseScreen.ShowPauseScreen();
		}
	}

}
