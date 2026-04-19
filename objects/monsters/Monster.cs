using Godot;

public partial class Monster : RigidBody3D
{
    [Export] public AnimationPlayer AnimPlayer;
    [Export] public Node3D MonsterModel;
    [Export] public AudioStreamPlayer3D HurtSound;
    [Export] public AudioStreamPlayer3D DieSound;
    [Export] public AudioStreamPlayer3D AttackSound;
    [Export] public AudioStreamPlayer3D AlertSound;
    [Export] public GpuParticles3D HitParticles;

    [ExportGroup("Stats")]
    [Export] public float MaxHealth     = 100f;
    
    [ExportGroup("Range")]
    [Export] public float AggroRange    = 10f;   // meters — starts chasing
    [Export] public float AttackRange   = 0.8f;  // meters — starts attacking

    [ExportGroup("Movement")]
    [Export] public float Speed      = 2f;  // meters/sec horizontal cap

    [ExportGroup("Combat")]
    [Export] public float AttackInterval = 1f; // seconds between attacks
    [Export] public float AttackDamage   = 20f; 
    [Export] public float HurtStunTime = 0.2f; // seconds to pause after getting hit
    [Export] public string AttackType = "melee"; // "melee", "ranged", or "magic"
    [Export] public PackedScene RangedProjectileScene; // for ranged attacks
    [Export] public Node3D ProjectileSpawnPoint;

    [ExportGroup("Death")]
    [Export] public float DeathLingerTime = 2.0f; // seconds before QueueFree

    [ExportGroup("Animation Names")]
    [Export] public string AnimIdle   = "idle";
    [Export] public string AnimWalk   = "walk";
    [Export] public string[] AnimAttacks = { "attack-melee-left", "attack-melee-right", "attack-kick-left", "attack-kick-right" };
    [Export] public string AnimDie    = "die";
    [Export] public string AnimHurt   = "custom/hurt";

    // -------------------------------------------------------------------------
    // State machine
    // -------------------------------------------------------------------------
    private enum State { Idle, Chase, Attack, Dead, Hurt }

    private State _state = State.Idle;

    // -------------------------------------------------------------------------
    // Private fields
    // -------------------------------------------------------------------------
    private float _health;
    private float _attackCooldown  = 0f;
    private float _deathTimer      = 0f;
    private float _stunTimer      = 0f;
    private string _currentAnim    = "";


    public override void _Ready()
    {
        _health = MaxHealth;

        PlayAnim(AnimIdle);
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        // Dead state is handled separately — just count down then free.
        if (_state == State.Dead)
        {
            _deathTimer -= dt;
            if (_deathTimer <= 0f)
                QueueFree();
            return;
        }

        float distToPlayer = GlobalPosition.DistanceTo(GameManager.Player.GlobalPosition);

        // Tick attack cooldown every frame regardless of state.
        if (_attackCooldown > 0f)
            _attackCooldown -= dt;

        // -----------------------------------------------------------------
        // State transitions
        // -----------------------------------------------------------------
        switch (_state)
        {
            case State.Idle:
                if (distToPlayer <= AggroRange)
                {    
                    AlertSound?.Play();
                    EnterChase();
                }
                break;

            case State.Chase:
                if (distToPlayer <= AttackRange)
                    EnterAttack();
                else
                    ChasePlayer();
                break;

            case State.Attack:
                if (distToPlayer > AttackRange)
                    EnterChase();
                else if (_attackCooldown <= 0f)
                    PerformAttack();
                break;
            case State.Hurt:
                _stunTimer -= dt;
                if (_stunTimer <= 0f)
                {
                    if (distToPlayer > AttackRange)
                        EnterChase();
                    else    
                        EnterAttack();
                }
                break;
        }

        // Face the player in most states except...
        if(_state != State.Idle && _state != State.Dead)
            FacePlayer();
    }

    // -------------------------------------------------------------------------
    // State handlers
    // -------------------------------------------------------------------------
    public void EnterChase()
    {
        _state = State.Chase;
        PlayAnim(AnimWalk);
    }

    private void ChasePlayer()
    {
        Vector3 toPlayer = (GameManager.Player.GlobalPosition - GlobalPosition);
        toPlayer.Y = 0f;

        if (toPlayer.LengthSquared() < 0.001f)
            return;

        toPlayer = toPlayer.Normalized();
        LinearVelocity = toPlayer * Speed;
    }

    private void EnterAttack()
    {
        _state = State.Attack;
    }

    private void PerformAttack()
    {
        if (AttackType == "melee")
        {
            PerformMeleeAttack();
        }
        else if (AttackType == "ranged")
        {
            PerformRangedAttack();
        }
        else if (AttackType == "magic")
        {
            GD.PrintErr("Magic attack not implemented yet.");
        }
        else
        {
            GD.PrintErr($"Unknown attack type: {AttackType}");
        }
    }

    private void PerformMeleeAttack()
    {
        _attackCooldown = AttackInterval;
        var attackAnim = AnimAttacks[GD.Randi() % AnimAttacks.Length];
        PlayAnim(attackAnim, true);
        
        GD.Print($"{Name} attacks player for {AttackDamage} damage!");
        GameManager.Player.TakeDamage(AttackDamage);
        AttackSound?.Play();
    }

    private void PerformRangedAttack()
    {
        _attackCooldown = AttackInterval;
        var attackAnim = AnimAttacks[GD.Randi() % AnimAttacks.Length];
        PlayAnim(attackAnim, true);

        var projectile = RangedProjectileScene.Instantiate<Node3D>();
        GetTree().CurrentScene.AddChild(projectile);

        projectile.GlobalTransform = ProjectileSpawnPoint.GlobalTransform;

        GD.Print($"{Name} shoots a projectile at the player!");

        AttackSound?.Play();
    }

    private void EnterHurt()
    {
        _state = State.Hurt;
        _stunTimer = HurtStunTime;
        PlayAnim(AnimHurt, true);
        HurtSound?.Play();
    }   

    // -------------------------------------------------------------------------
    // Damage / death
    // -------------------------------------------------------------------------

    /// <summary>
    /// Call this from the Bullet script (or an Area3D signal) to deal damage.
    /// </summary>
    public void TakeDamage(float amount, Vector3 hitPoint)
    {
        if (_state == State.Dead)
            return;

        _health -= amount;
        GD.Print($"{Name} took {amount} damage — health: {_health}/{MaxHealth}");

        if (HitParticles != null)
        {
            HitParticles.GlobalPosition = hitPoint;
            HitParticles.Emitting = false; // Restart the particle effect
            HitParticles.Emitting = true;
        }

        if (_health <= 0f)
            Die();
        else
        {
            EnterHurt();
        }    
    }

    private void Die()
    {
        _state       = State.Dead;
        _deathTimer  = DeathLingerTime;

        // Turn off gravity for the body
        GravityScale = 0f;

        // Disable collision so bullets / player pass through the corpse.
        SetDeferred("collision_layer", 0);
        SetDeferred("collision_mask",  0);

        PlayAnim(AnimDie);
        DieSound?.Play();
        GD.Print($"{Name} has died.");
    }

    /// <summary>Rotate the monster to face the player on the Y axis only.</summary>
    private void FacePlayer()
    {
        Vector3 direction = GameManager.Player.GlobalPosition - GlobalPosition;
        direction.Y = 0f;

        if (direction.LengthSquared() < 0.001f)
            return;

        MonsterModel.GlobalTransform = MonsterModel.GlobalTransform.LookingAt(
            GlobalPosition + direction, Vector3.Up);
    }

    /// <summary>Play an animation only if it isn't already playing.</summary>
    private void PlayAnim(string animName, bool force = false)
    {
        if (AnimPlayer == null || (_currentAnim == animName && !force))
            return;

        _currentAnim = animName;
        AnimPlayer.Play(animName);
    }
}
