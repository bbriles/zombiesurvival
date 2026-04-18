using Godot;
using System;

public partial class DeathScreen : Control
{
	public override void _Ready()
    {
        // Hide on startup, shown when player dies
        Hide();
    }

    public void ShowDeathScreen()
    {
        Show();
        // Ungrab mouse so buttons are clickable
        Input.MouseMode = Input.MouseModeEnum.Visible;
        // Pause the game so the player stops simulating
        GetTree().Paused = true;
    }

    private void OnRestartPressed()
    {
        GetTree().Paused = false;
        Input.MouseMode = Input.MouseModeEnum.Captured;
        //GetTree().ChangeSceneToFile(GameScenePath);
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
