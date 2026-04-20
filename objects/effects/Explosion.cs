using Godot;

public partial class Explosion : Node3D
{
    [Export] public MeshInstance3D ExplosionMesh { get; set; }
    [Export] public OmniLight3D ExplosionLight { get; set; }
    [Export] public GpuParticles3D ExplosionParticles { get; set; }
    [Export] public CollisionShape3D ExplosionCollision { get; set; }
	[Export] public float ExplosionDamage { get; set; } = 10.0f;
    [Export] public float KnockbackForce { get; set; } = 50.0f;

    [Export] public float Lifetime { get; set; } = 0.28f;
    [Export] public float ExpandSpeed { get; set; } = 15.0f;

    private float _timer = 0f;
    private float _initialLightEnergy;

    public override void _Ready()
    {
        if (ExplosionMesh != null)
        {
            Aabb meshBounds = ExplosionMesh.GetAabb();
            float radius = meshBounds.Size.X * 0.5f;
        }

        if (ExplosionLight != null)
            _initialLightEnergy = ExplosionLight.LightEnergy;

        if (ExplosionParticles != null)
            ExplosionParticles.Emitting = true;
    }

    public override void _Process(double dt)
    {
        _timer += (float)dt;
        float progress = _timer / Lifetime;

        if (ExplosionMesh != null)
        {
            float scale = 1f + ExpandSpeed * progress;
            ExplosionMesh.Scale = Vector3.One * scale;
			

            if (ExplosionCollision != null)
                ExplosionCollision.Scale = ExplosionMesh.Scale;
        }

        if (ExplosionMesh != null)
        {
            var material = ExplosionMesh.GetActiveMaterial(0) as StandardMaterial3D;
            if (material != null)
            {
                material.AlbedoColor = material.AlbedoColor with
                {
                    A = Mathf.Lerp(1f, 0f, progress)
                };
            }
        }

        if (ExplosionLight != null)
            ExplosionLight.LightEnergy = Mathf.Lerp(_initialLightEnergy, 0f, progress);

        if (_timer >= Lifetime)
            QueueFree();
    }

	public void OnBodyEntered(Node body)
	{
		GD.Print("Explosion hit: " + body.Name);
		if (body is Monster monster)
		{
			monster.TakeDamage(ExplosionDamage, GlobalPosition);
			monster.ApplyKnockback(GlobalPosition, KnockbackForce);
		}
		else if (body is Player player)
		{
			player.TakeDamage(ExplosionDamage);
		}
	}
}