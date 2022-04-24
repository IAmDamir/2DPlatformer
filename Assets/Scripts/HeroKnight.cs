using System;
using UnityEngine;

/*
    TODO: Make Player block only attacks the direction player looks
 */

public class HeroKnight : MonoBehaviour, HealthPoints<int>
{
    //                                              Restart Position
    [SerializeField] private Vector3                startingPosition;
    //                                              Variales used for jump mechanic
    [SerializeField] private Sensor_HeroKnight      m_groundSensor;
    private float                                   m_jumpForce = 7.5f;
    private bool                                    m_grounded = false;
    //                                              Variables used for attack mechanic
    [SerializeField] private LayerMask              enemyLayers;
    [SerializeField] private Transform              attackPoint;
    [SerializeField] private float                  attackRange = 0.75f;
    [SerializeField] private int                    attackDamage = 1;
    private float                                   nextComboTime = 0f;
    private float                                   m_timeSinceAttack = 0.0f;
    private bool                                    m_blocking = false;
    private int                                     m_currentAttack = 0;
    //                                              Variables used for Health system
    [SerializeField] private int                    maxHealth;
    public int                                      currentHealth { get; set; }
    //                                              Variables used for roll mechanic
    [SerializeField] private float                  m_rollForce = 10.0f;
    private float                                   m_rollDuration = 8.0f / 14.0f;
    private float                                   m_rollCurrentTime;
    private bool                                    m_rolling = false;
    //                                              Movement speed
    [SerializeField] private float                  m_speed = 4.0f;
    //                                              Rigidbody
    private Rigidbody2D                             m_body2d;
    //                                              Animator (used for changing between animations)
    private Animator                                m_animator;
    //                                              Variable used for preventing flickering animation to Idle state
    private float                                   m_delayToIdle = 0.0f;
    //                                              Rolling direction
    private int                                     m_facingDirection = 1;
    //                                              Using new Input System
    private Platformer                              controller;
    private float                                   inputX;
    //                                              audioSource (used for making sounds)
    private AudioSource                             audioSource;
    //                                              different clips that play when action performed
    public AudioClip[]                              hurtClips;
    public AudioClip[]                              deathClips;
    public AudioClip[]                              blockClips;
    public AudioClip[]                              attackClips;
    public AudioClip                                shieldUpClip;
    public AudioClip                                rollClip;
    public AudioClip                                footstepClip;
    public AudioClip                                whiffClip;

    void Awake()
    {
        audioSource = gameObject.GetComponent<AudioSource>();

        controller = new Platformer();
        
        // Binding functions to buttons/sticks/etc.
        // Performed will only call function once after button is pressed
        // If you want to continiously call function you need to write the logic inside Update/FixedUpdate/etc.
        controller.Player.Attack.performed += _ => Attack();
        controller.Player.Roll.performed += _ => Roll();
        controller.Player.Block.performed += _ => Block();
        controller.Player.Block.canceled += _ => StopBlock();
        controller.Player.Jump.performed += _ => Jump();
    }

    private void OnEnable()
    {
        // Enables our controller
        controller.Enable();
    }

    private void OnDisable()
    {
        // Disables our controller
        controller.Disable();
    }

    // Use this for initialization
    void Start()
    {
        m_animator = GetComponent<Animator>();
        m_body2d = GetComponent<Rigidbody2D>();
        m_groundSensor = transform.Find("GroundSensor").GetComponent<Sensor_HeroKnight>();
        currentHealth = maxHealth;
    }
    // Update is called once per frame
    void Update()
    {
        // Prevents flickering transitions to idle
        m_delayToIdle -= Time.deltaTime;
        if (m_delayToIdle < 0)
        {
            m_animator.SetInteger("AnimState", 0);
        }

        // Increase timer that controls attack combo
        m_timeSinceAttack += Time.deltaTime;

        // Disable rolling if timer extends duration
        if (Time.time > m_rollCurrentTime)
        {
            Physics2D.IgnoreLayerCollision(6, 7, false);
            m_rolling = false;
        }

        //Check if character just landed on the ground
        if (!m_grounded && m_groundSensor.State())
        {
            m_grounded = true;
            m_animator.SetBool("Grounded", m_grounded);
        }

        //Check if character just started falling
        if (m_grounded && !m_groundSensor.State())
        {
            m_grounded = false;
            m_animator.SetBool("Grounded", m_grounded);
        }

        //Set AirSpeed in animator
        m_animator.SetFloat("AirSpeedY", m_body2d.velocity.y);

        // Handle movement
        inputX = controller.Player.Move.ReadValue<Vector2>().x;

        if (inputX != 0)
        {
            Move();
        } else if (m_body2d.velocity.x != 0 && m_grounded && !m_rolling)
        {
            m_body2d.AddForce((-m_body2d.velocity) * Time.deltaTime * 150);
        }
    }

    private void Move()
        {
        if (!m_rolling && !m_blocking)
        {
            // Swap direction of sprite depending on walk direction
            Flip(inputX);

            // Move
            if (!m_rolling)
                m_body2d.velocity = new Vector2(inputX * m_speed, m_body2d.velocity.y);

            // Reset timer
            m_delayToIdle = 0.05f;
            m_animator.SetInteger("AnimState", 1);

            if(!audioSource.isPlaying && m_body2d.velocity.magnitude > m_speed-1 && m_grounded)
            {
                audioSource.clip = footstepClip;
                audioSource.volume = UnityEngine.Random.Range(0.8f, 1);
                audioSource.pitch = UnityEngine.Random.Range(0.8f, 1.1f);
                audioSource.Play();
            }
        }
    }
    private void Flip(float direction)
    {
        if (direction > 0 && !m_blocking)
        {
            GetComponent<SpriteRenderer>().flipX = false;
            m_facingDirection = 1;
        }

        else if (direction < 0 && !m_blocking)
        {
            GetComponent<SpriteRenderer>().flipX = true;
            m_facingDirection = -1;
        }
    }
    private void Jump()
    {
        if (m_grounded && !m_rolling && !m_blocking)
        {
            m_animator.SetTrigger("Jump");
            m_grounded = false;
            m_animator.SetBool("Grounded", m_grounded);
            m_body2d.velocity = new Vector2(m_body2d.velocity.x, m_jumpForce);
            m_groundSensor.Disable(0.2f);
        }
    }
    private void Roll()
    {
        if (!m_rolling && !m_blocking)
        {
            m_rolling = true;
            m_rollCurrentTime = Time.time + m_rollDuration;
            Physics2D.IgnoreLayerCollision(6, 7);
            m_animator.SetTrigger("Roll");
            audioSource.clip = rollClip;
            audioSource.Play();
            m_body2d.velocity = new Vector2(m_facingDirection * m_rollForce, m_body2d.velocity.y);
        }
    }
    private void Block()
    {
        if (!m_rolling)
        {
            m_animator.SetTrigger("Block");

            audioSource.clip = shieldUpClip;
            audioSource.Play();

            m_animator.SetBool("IdleBlock", true);
            m_blocking = true;
            m_speed = 0.0f;
        }
    }
    private void StopBlock()
    {
        m_animator.SetBool("IdleBlock", false);
        m_blocking = false;
        m_speed = 4.0f;
    }
    private void Attack()
    {
        if (m_timeSinceAttack > 0.25f && !m_rolling)
        {
            if (Time.time > nextComboTime)
            {
                // Reset Attack combo if time since last attack is too large
                if (m_timeSinceAttack > 1.0f)
                {
                    m_currentAttack = 0;
                }

                m_currentAttack++;

                // Loop back to one after third attack
                if (m_currentAttack > 2)
                {
                    nextComboTime = Time.time + 1f;
                }

                // Call one of three attack animations "Attack1", "Attack2", "Attack3"
                m_animator.SetTrigger("Attack" + m_currentAttack);

                // Reset timer
                m_timeSinceAttack = 0.0f;

                // Detect enemies in range of attack
                Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

                if(hitEnemies.Length == 0)
                {
                    audioSource.clip = whiffClip;
                    audioSource.pitch = UnityEngine.Random.Range(0.8f, 1.1f);
                    audioSource.volume = UnityEngine.Random.Range(0.8f, 1);
                } else
                {
                    audioSource.clip = attackClips[m_currentAttack - 1];
                }

                // Damage enemies
                foreach (Collider2D enemy in hitEnemies)
                {
                    if (enemy.GetComponent<Bandit>() != null)
                    {
                        enemy.GetComponent<Bandit>().TakeDamage(attackDamage);
                    }
                }

                audioSource.Play();
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (!m_blocking && !m_rolling)
        {
            currentHealth -= damage;

            if (currentHealth <= 0)
            {
                Die();
            } else
            {
                m_animator.SetTrigger("Hurt");

                playRandomClip(hurtClips);
            }
        }
        else if (m_blocking)
        {
            m_animator.SetTrigger("Block");

            playRandomClip(blockClips);
        }
    }
    public void Die()
    {
        m_animator.SetTrigger("Death");

        playRandomClip(deathClips);

        transform.position = startingPosition;

        currentHealth = maxHealth;
    }

    private void playRandomClip(AudioClip[] clips)
    {
        int index = UnityEngine.Random.Range(0, clips.Length);
        audioSource.clip = clips[index];
        audioSource.Play();
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
        {
            return;
        }

        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
