using Godot;
using System;

public partial class Weapon : Node3D
{
	[Export] public AnimationPlayer WeaponAnim;
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

	private float _fireCooldown = 0f;
	private float _lightCounter = 0f;
	private float _bobTime = 0f;

	private Vector3 _initialWeaponPosition;

	public override void _Ready()
	{
		// Cache the resting position of the weapon
		_initialWeaponPosition = Position;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		if (_fireCooldown > 0f)
			_fireCooldown -= dt;
		if (_lightCounter > 0f)
			_lightCounter -= dt;
		else if (MuzzleFlashLight != null && MuzzleFlashLight.Visible)
			MuzzleFlashLight.Visible = false;
	}

	public void Shoot()
	{
		if (_fireCooldown > 0f)
			return;

		_fireCooldown = FireRate;

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

	public void ApplyWeaponBob(bool isMoving, float dt)
	{
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
		Position = Position.Lerp(targetPosition, dt * BobLerpSpeed);
	}
}
