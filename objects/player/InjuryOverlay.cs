using Godot;

public partial class InjuryOverlay : ColorRect
{
	[Export] public float HealSpeed { get; set; } = 0.8f;   // how fast damage fades
    [Export] public float FlashSpeed { get; set; } = 8.0f;  // how fast damage flashes in

    private ShaderMaterial _material;
    private float _damageAmount = 0f;
    private float _targetDamage = 0f;

    public override void _Ready()
    {
        _material = (ShaderMaterial)Material;
        // Make sure it doesn't block input
        MouseFilter = MouseFilterEnum.Ignore;
        UpdateShader();
    }

    public override void _Process(double delta)
    {
        if (Mathf.IsEqualApprox(_damageAmount, _targetDamage, 0.001f))
            return;

        // Flash in quickly, heal out slowly
        float speed = _damageAmount < _targetDamage ? FlashSpeed : HealSpeed;
        _damageAmount = Mathf.Lerp(_damageAmount, _targetDamage, (float)delta * speed);

        // Snap to zero to fully clear the overlay
        if (_damageAmount < 0.01f)
            _damageAmount = 0f;

        UpdateShader();
    }

    /// <summary>
    /// Call this from your Player script when damage is taken.
    /// severity: 0.0 (scratch) to 1.0 (near death)
    /// </summary>
    public void TakeDamage(float severity)
    {
        // Always flash to at least the new severity, never reduce on hit
        _targetDamage = Mathf.Clamp(Mathf.Max(_targetDamage, severity), 0f, 1f);
    }

    /// <summary>
    /// Call this to gradually heal the overlay (e.g. on a timer or regen tick).
    /// </summary>
    public void Heal(float amount)
    {
        _targetDamage = Mathf.Clamp(_targetDamage - amount, 0f, 1f);
    }

    /// <summary>
    /// Instantly clear the overlay (e.g. on respawn).
    /// </summary>
    public void Reset()
    {
        _damageAmount = 0f;
        _targetDamage = 0f;
        UpdateShader();
    }

    private void UpdateShader()
    {
        _material.SetShaderParameter("damage_amount", _damageAmount);
    }
}

