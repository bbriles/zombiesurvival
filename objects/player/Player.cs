using Godot;

/// <summary>
/// DOOM-style player controller for a CharacterBody3D.
///
/// Controls:
///   W / Up Arrow      — Move forward
///   S / Down Arrow    — Move backward
///   A / Left Arrow    — Turn left  (rotate camera/body on Y axis)
///   D / Right Arrow   — Turn right (rotate camera/body on Y axis)
///   Alt + A / Alt + D — Strafe left / right
///   Space             — Jump
///   Shift             — Run
/// </summary>
public partial class Player : CharacterBody3D
{
	[Export] public Camera3D Camera;
	[Export] public AnimationPlayer WeaponAnim;

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
	[Export] public AudioStreamPlayer ShootSound;
	[Export] public GpuParticles3D MuzzleFlash;
	[Export] public SpotLight3D MuzzleFlashLight;
	[Export] public float MuzzleFlashLightLength = 0.1f;
	[Export] public RayCast3D WeaponRay;
	[Export] public float WeaponDamage = 25f;
	[Export] public float FireRate = 0.25f;  // seconds between shots
	[Export] public float BobFrequency = 1.5f;   // cycles per second
	[Export] public float BobAmplitudeY = 0.02f; // vertical height
	[Export] public float BobAmplitudeX = 0.01f; // horizontal drift
	[Export] public float BobLerpSpeed = 10.0f;  // smoothing speed

	[ExportGroup("Throwing")]
	[Export] public PackedScene GrenadeScene;
	[Export] public Node3D GrenadeThrowPoint;

	[ExportGroup("Health")]
	[Export] public InjuryOverlay InjuryOverlay;
	[Export] public DeathScreen DeathScreen;
	[Export] public float MaxHealth = 100f;
	[Export] public float CurrentHealth = 100f;
	[Export] public float HealAmount = 5f; // amount to heal on each timer tick

	private float _gravity;
	private float _fireCooldown = 0f;
	private float _lightCounter = 0f;
	private Vector3 _initialWeaponPosition;
	private float _bobTime = 0f;

	public override void _Ready()
	{
		GameManager.Player = this;
		
		// Cache project gravity so we don't call the server every frame.
		_gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");

		// Cache the resting position of the weapon
		_initialWeaponPosition = WeaponPivot.Position;
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
		if (_fireCooldown > 0f)
			_fireCooldown -= dt;
		if (_lightCounter > 0f)
			_lightCounter -= dt;
		else if (MuzzleFlashLight != null && MuzzleFlashLight.Visible)
			MuzzleFlashLight.Visible = false;
		if (Input.IsActionJustPressed("shoot") && _fireCooldown <= 0f)
		{
			Shoot();
			_fireCooldown = FireRate;
		}
		
		// -----------------------------------------------------------------
		// 9. Weapon Bobbing and Sway
		// -----------------------------------------------------------------
		 ApplyWeaponBob((float)dt);
		
		// -----------------------------------------------------------------
		// 10. Grenade Throwing
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

	private void Shoot()
	{
		WeaponAnim?.Play("custom/shoot");
		ShootSound?.Play();

		if(MuzzleFlash != null && MuzzleFlashLight != null)
		{ 
			MuzzleFlash.Emitting = true;
			MuzzleFlashLight.Visible = true;
			_lightCounter = MuzzleFlashLightLength;
		}
		
		if (WeaponRay == null || !WeaponRay.IsColliding())
			return;

		Node collider = WeaponRay.GetCollider() as Node;

		if (collider is Monster monster)
			monster.TakeDamage(WeaponDamage, WeaponRay.GetCollisionPoint());
	}

	private void ThrowGrenade()
	{
		if (GrenadeScene == null || GrenadeThrowPoint == null)
			return;

		var grenade = GrenadeScene.Instantiate<Grenade>();
        GetTree().CurrentScene.AddChild(grenade);

        grenade.GlobalTransform = GrenadeThrowPoint.GlobalTransform;
		grenade.Throw(-GrenadeThrowPoint.GlobalTransform.Basis.Z); // Forward direction of the throw point
        
		GD.Print($"{Name} throws a grenade!");
	}

	private void ApplyWeaponBob(float dt)
	{
		// Only bob on horizontal movement
		float speed = new Vector2(Velocity.X, Velocity.Z).Length();
		bool isMoving = speed > 0.1f && IsOnFloor();

		Vector3 targetPosition;

		if (isMoving)
		{
			_bobTime += dt * BobFrequency * Mathf.Pi * 2f;

			float bobY = Mathf.Sin(_bobTime) * BobAmplitudeY;
			// X uses a doubled frequency for a figure-8 style sway
			float bobX = Mathf.Sin(_bobTime * 0.5f) * BobAmplitudeX;

			targetPosition = _initialWeaponPosition + new Vector3(bobX, bobY, 0f);
		}
		else
		{
			// Smoothly reset _bobTime toward the nearest full cycle to avoid snapping
			_bobTime = Mathf.Lerp(_bobTime, Mathf.Round(_bobTime / (Mathf.Pi * 2f)) * Mathf.Pi * 2f, dt * BobLerpSpeed);
			targetPosition = _initialWeaponPosition;
		}

		// Lerp for smooth transition in and out of bobbing
		WeaponPivot.Position = WeaponPivot.Position.Lerp(targetPosition, dt * BobLerpSpeed);
	}

	public void OnHealTimerTimeOut()
	{
		CurrentHealth = Mathf.Clamp(CurrentHealth + HealAmount, 0f, MaxHealth);
		InjuryOverlay.Heal(HealAmount / MaxHealth); // Convert heal amount to severity (0.0 to 1.0)	
	}

}
