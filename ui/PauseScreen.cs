using Godot;
using System;

public partial class PauseScreen : Control
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Hide on startup, shown when paused
        Hide();
	}

	public void ShowPauseScreen()
	{
		Show();
		// Ungrab mouse so buttons are clickable
		Input.MouseMode = Input.MouseModeEnum.Visible;
		// Pause the game so the player stops simulating
		GetTree().Paused = true;
	}

	private void OnResumePressed()
	{
		GD.Print("Resuming game...");
		GetTree().Paused = false;
		Input.MouseMode = Input.MouseModeEnum.Captured;
		Hide();
	}

	private void OnQuitPressed()
	{
		GD.Print("Quitting game...");
		GetTree().Quit();
	}
}
