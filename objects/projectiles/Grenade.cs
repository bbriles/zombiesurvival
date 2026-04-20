using Godot;
using System;

public partial class Grenade : RigidBody3D
{
	[Export] public PackedScene ExplosionScene;
	[Export] public float ThrowForce { get; set; } = 6.0f;
    [Export] public float UpwardAngle { get; set; } = 0.3f;

    private bool _hasExploded = false;

	public void Throw(Vector3 direction)
	{
		Vector3 throwDirection = direction.Normalized();
		throwDirection.Y += UpwardAngle; // Add upward angle to the throw
		ApplyCentralImpulse(throwDirection * ThrowForce);
	}

	private void OnBodyEntered(Node body)
	{
		GD.Print("Grenade collided with: " + body.Name);
		if (_hasExploded)
			return;

		_hasExploded = true;

		// Spawn explosion effect
		if (ExplosionScene != null)
		{
			var explosion = ExplosionScene.Instantiate<Explosion>();
			GetTree().CurrentScene.AddChild(explosion);
			explosion.GlobalPosition = GlobalPosition;
		}

		QueueFree();
	}
}
