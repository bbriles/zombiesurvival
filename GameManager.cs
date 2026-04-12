using Godot;

public partial class GameManager : Node
{
    // Global singleton instance, accessible from anywhere.
    public static GameManager Instance { get; private set; }

    public static Player Player { get; set; }

    public override void _Ready()
    {
        Instance = this;
    }
}