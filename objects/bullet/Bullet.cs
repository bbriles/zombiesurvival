using Godot;
using System;

public partial class Bullet : Area3D
{
	[ExportGroup("Projectile")]
	/// Travel speed in metres per second.
	[Export] public float Speed = 40.0f;

	/// Maximum distance (metres) before the bullet is destroyed.
	[Export] public float MaxRange = 100.0f;

	[Export]
	public float Damage = 25.0f;

	// -------------------------------------------------------------------------
	// Private state
	// -------------------------------------------------------------------------

	private Vector3 _spawnPosition;

	// -------------------------------------------------------------------------
	// Godot lifecycle
	// -------------------------------------------------------------------------

	public override void _Ready()
	{
		// Record where we started so we can measure distance travelled.
		_spawnPosition = GlobalPosition;

		// Connect hit signals to local handlers.
		BodyEntered  += OnBodyEntered;
		AreaEntered  += OnAreaEntered;
	}

	public override void _PhysicsProcess(double delta)
	{
		// Move forward in local -Z each frame.
		GlobalPosition += -GlobalTransform.Basis.Z * Speed * (float)delta;

		// Destroy once the bullet exceeds its maximum range.
		if (GlobalPosition.DistanceTo(_spawnPosition) >= MaxRange)
			DestroyBullet();
	}

	// -------------------------------------------------------------------------
	// Hit detection
	// -------------------------------------------------------------------------

	private void OnBodyEntered(Node3D body)
	{
		if (body is Monster monster)
			monster.TakeDamage(Damage, Vector3.Zero); // TODO: pass actual hit point from collision

		GD.Print($"Bullet hit body: {body.Name}");
		DestroyBullet();
	}

	private void OnAreaEntered(Area3D area)
	{
		// Optional: react to overlapping areas (e.g. trigger zones).
		// Remove or leave empty if you only care about physics bodies.
		GD.Print($"Bullet entered area: {area.Name}");
		DestroyBullet();
	}

	// -------------------------------------------------------------------------
	// Helpers
	// -------------------------------------------------------------------------

	/// <summary>
	/// Central teardown point — spawn effects here before freeing if needed.
	/// </summary>
	private void DestroyBullet()
	{
		// Guard against being called twice in the same frame (e.g. range limit
		// fires at the same physics step as a collision).
		if (!IsInsideTree())
			return;

		// TODO: optionally spawn an impact / puff particle scene here.
		// Example:
		//   var puff = ImpactScene.Instantiate<Node3D>();
		//   GetTree().Root.AddChild(puff);
		//   puff.GlobalPosition = GlobalPosition;

		QueueFree();
	}
}
