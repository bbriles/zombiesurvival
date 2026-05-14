using Godot;

/// <summary>
/// DOOM-style player controller for a CharacterBody3D.
/// </summary>
public partial class Player : CharacterBody3D
{
	[Export] public Camera3D Camera;

	[ExportGroup("Movement")]
	[Export] public float WalkSpeed    = 2.5f;
	[Export] public float RunSpeed     = 5.0f;
	[Export] public float StrafeSpeed  = 2.0f;
	[Export] public float JumpVelocity = 3.0f;

	[ExportGroup("Turning")]
	/// <summary>Keyboard turn speed in radians per second.</summary>
	[Export] public float TurnSpeed = 2.2f;

	[ExportGroup("Shooting")]
	[Export] public Node3D WeaponPivot;
	[Export] public Weapon CurrentWeapon;

	[ExportGroup("Throwing")]
	[Export] public PackedScene ThrowObjectScene;
	[Export] public Node3D ThrowPoint;

	[ExportGroup("Health")]
	[Export] public InjuryOverlay InjuryOverlay;
	[Export] public DeathScreen DeathScreen;
	[Export] public float MaxHealth = 100f;
	[Export] public float CurrentHealth = 100f;
	[Export] public float HealAmount = 5f; // amount to heal on each timer tick
	[ExportGroup("Items")]
	[Export] public int GrenadeCount = 0;
	[Signal] public delegate void ItemPickupEventHandler();
	[Signal] public delegate void ItemUsedEventHandler();

	private float _gravity;

	public override void _Ready()
	{
		GameManager.Player = this;
		
		// Cache project gravity so we don't call the server every frame.
		_gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");

		
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		Vector3 vel = Velocity;

		// -----------------------------------------------------------------
		// 1. Gravity
		// -----------------------------------------------------------------
		if (!IsOnFloor())
			vel.Y -= _gravity * dt;

		// -----------------------------------------------------------------
		// 2. Jump
		// -----------------------------------------------------------------
		if (Input.IsActionJustPressed("jump") && IsOnFloor())
			vel.Y = JumpVelocity;

		// -----------------------------------------------------------------
		// 3. Turning — rotates the whole body (DOOM style, no mouse look)
		// -----------------------------------------------------------------
		bool strafeToggle = Input.IsActionPressed("strafe_toggle");

		if (!strafeToggle)
		{
			float turn = 0f;
			if (Input.IsActionPressed("turn_left"))  turn += 1f;
			if (Input.IsActionPressed("turn_right")) turn -= 1f;

			if (turn != 0f)
				RotateY(turn * TurnSpeed * dt);
		}

		// -----------------------------------------------------------------
		// 4. Forward / backward movement
		// -----------------------------------------------------------------
		bool isRunning = Input.IsActionPressed("run");
		float speed = isRunning ? RunSpeed : WalkSpeed;

		float moveZ = 0f;
		if (Input.IsActionPressed("forward"))  moveZ -= 1f;
		if (Input.IsActionPressed("backward")) moveZ += 1f;

		// -----------------------------------------------------------------
		// 5. Strafing (Alt held, or pure A/D with no turn)
		// -----------------------------------------------------------------
		float moveX = 0f;
		if (strafeToggle)
		{
			// Alt modifier forces A/D into strafe mode.
			if (Input.IsActionPressed("turn_left"))  moveX -= 1f;
			if (Input.IsActionPressed("turn_right")) moveX += 1f;
		}

		// -----------------------------------------------------------------
		// 6. Build horizontal velocity in local space then transform to world
		// -----------------------------------------------------------------
		Vector3 localMove = new Vector3(moveX * StrafeSpeed, 0, moveZ * speed);

		// Transform direction from local body space to world space.
		Vector3 worldMove = GlobalTransform.Basis * localMove;

		vel.X = worldMove.X;
		vel.Z = worldMove.Z;

		// -----------------------------------------------------------------
		// 7. Apply and move
		// -----------------------------------------------------------------
		Velocity = vel;
		MoveAndSlide();

		// -----------------------------------------------------------------
		// 8. Shooting
		// -----------------------------------------------------------------
		if (Input.IsActionJustPressed("shoot") && CurrentWeapon != null)
		{
			CurrentWeapon.Shoot();
		}
		
		ApplyWeaponBob(dt);
		
		// -----------------------------------------------------------------
		// 9. Grenade Throwing
		// -----------------------------------------------------------------
		if (Input.IsActionJustPressed("throw"))
			ThrowGrenade();
	}

	public void TakeDamage(float amount)
	{
		CurrentHealth = Mathf.Clamp(CurrentHealth - amount, 0f, MaxHealth);
		float severity = 1f - (CurrentHealth / MaxHealth);
		InjuryOverlay.TakeDamage(severity);

		if (CurrentHealth <= 0f)
			Die();
	}

	private void Die()
	{
		// TODO: play sound, etc.
		DeathScreen.ShowDeathScreen();
	}

	private void ThrowGrenade()
	{
		if(ThrowObjectScene == null)
		{
			GD.PrintErr("ThrowObjectScene is not assigned!");
			return;
		}
		GD.Print($"{Name} attempts to throw a grenade...");		
		if (ThrowObjectScene == null || ThrowPoint == null || GrenadeCount <= 0)
			return;

		var grenade = ThrowObjectScene.Instantiate<Grenade>();
        GetTree().CurrentScene.AddChild(grenade);

        grenade.GlobalTransform = ThrowPoint.GlobalTransform;
		grenade.Throw(-ThrowPoint.GlobalTransform.Basis.Z); // Forward direction of the throw point
        
		GrenadeCount--;
		EmitSignal(SignalName.ItemUsed);
		GD.Print($"{Name} throws a grenade!");
	}

	

	public void PickupItem(Item item)
	{
		switch (item.Type)
		{
			case Item.ItemType.Grenade:
				GrenadeCount += item.Amount;
				GD.Print($"{Name} picked up {item.Amount} grenades! Total: {GrenadeCount}");
				break;
			case Item.ItemType.HealthPack:
				CurrentHealth = Mathf.Clamp(CurrentHealth + item.Amount, 0f, MaxHealth);
				InjuryOverlay.Heal(item.Amount / MaxHealth); // Convert heal amount to severity (0.0 to 1.0)
				GD.Print($"{Name} picked up a health pack! Healed {item.Amount} health. Current health: {CurrentHealth}");
				break;
			default:
				GD.Print($"{Name} picked up an unknown item: {item.Type}");
				break;			
		}
		EmitSignal(SignalName.ItemPickup);
	}

	public void OnHealTimerTimeOut()
	{
		CurrentHealth = Mathf.Clamp(CurrentHealth + HealAmount, 0f, MaxHealth);
		InjuryOverlay.Heal(HealAmount / MaxHealth); // Convert heal amount to severity (0.0 to 1.0)	
	}

	private void ApplyWeaponBob(float dt)
	{
		if (CurrentWeapon == null)
			return;

		float speed = new Vector2(Velocity.X, Velocity.Z).Length();
		bool isMoving = speed > 0.1f && IsOnFloor();

		CurrentWeapon?.ApplyWeaponBob(isMoving, dt);
	}
}
