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
	[Export] public AudioStreamPlayer ShootSound;

	[ExportGroup("Movement")]
	[Export] public float WalkSpeed    = 5.0f;
	[Export] public float RunSpeed     = 10.0f;
	[Export] public float StrafeSpeed  = 4.0f;
	[Export] public float JumpVelocity = 3.0f;

	[ExportGroup("Turning")]
	/// <summary>Keyboard turn speed in radians per second.</summary>
	[Export] public float TurnSpeed = 2.2f;

	[ExportGroup("Shooting")]
	[Export] public RayCast3D WeaponRay;
	[Export] public float WeaponDamage = 25f;
	[Export] public float FireRate = 0.25f;  // seconds between shots

	private float _gravity;
	private float _fireCooldown = 0f;

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
		if (_fireCooldown > 0f)
			_fireCooldown -= dt;
		if (Input.IsActionJustPressed("shoot") && _fireCooldown <= 0f)
		{
			Shoot();
			_fireCooldown = FireRate;
		}
	}

	private void Shoot()
	{
		if (WeaponRay == null || !WeaponRay.IsColliding())
			return;

		Node collider = WeaponRay.GetCollider() as Node;

		if (collider is Monster monster)
			monster.TakeDamage(WeaponDamage);

		WeaponAnim?.Play("custom/shoot");
		ShootSound?.Play();
	}
}
