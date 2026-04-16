using Godot;
using System;

public partial class MonsterSpawner : Node3D
{
	[Export] public PackedScene MonsterScene;
	[Export] public Node3D SpawnPoint;
	[Export] public GpuParticles3D SpawnEffect;
	[Export] public AnimationPlayer SpawnAnimPlayer;
	[Export] public AudioStreamPlayer3D SpawnSound;
	[Export] public String SpawnAnimName = "open";
	[Export] public int SpawnQuantity = 1;
	[Export] public float SpawnRange = 5f;

	private bool _spawned = false;

	public void SpawnMonster()
	{
		if (MonsterScene == null || SpawnPoint == null)
		{
			GD.PrintErr("MonsterSpawner is missing MonsterScene or SpawnPoint reference.");
			return;
		}

		// Create the monster instance and add it to the scene.
		var monsterInstance = MonsterScene.Instantiate<Monster>();
		GetTree().Root.AddChild(monsterInstance);
		monsterInstance.GlobalPosition = SpawnPoint.GlobalPosition;

		monsterInstance.EnterChase();

		// Play spawn effect if assigned.
		if (SpawnEffect != null)
		{
			SpawnEffect.GlobalPosition = SpawnPoint.GlobalPosition;
			SpawnEffect.Emitting = false; // Restart the particle effect
			SpawnEffect.Emitting = true;
		}
		if(SpawnAnimPlayer != null && !string.IsNullOrEmpty(SpawnAnimName))
		{
			SpawnAnimPlayer.Play(SpawnAnimName);
		}
		if(SpawnSound != null)		
		{
			SpawnSound.GlobalPosition = SpawnPoint.GlobalPosition;
			SpawnSound.Play();	
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if(!_spawned)
		{
			var distanceToPlayer = GlobalPosition.DistanceTo(GameManager.Player.GlobalPosition);
			if (distanceToPlayer <= SpawnRange)
			{
				SpawnMonster();
				_spawned = true;
			}
		}
	}

	public void Reset()
	{
		_spawned = false;
	}

	public void OnSpawnTimerTimeout()
	{
		SpawnMonster();
		_spawned = true;
	}
}
