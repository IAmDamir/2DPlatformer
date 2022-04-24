    using UnityEngine;
    using System.Collections;

/*
    TODO: Make script to make enemy walk at the direction he watches
 */

public class Bandit : MonoBehaviour, HealthPoints<int> {

    //                                      Variables used for Attack mechanic
    [SerializeField] public Transform       attackPoint;
    [SerializeField] public LayerMask       playerLayer;
    [SerializeField] public float           attackRange = 0.7f;
    private float                           attack_cooldown = 15f/10f;
    private float                           next_attack = 0f;
    private int                             attackDamage = 1;
    //                                      Variables used for Health System
    [SerializeField] public int             maxHealth;
    public int                              currentHealth { get; set; }
    //                                      Variables used for AI movement (Patrooling, Chasing player, etc.)
    [SerializeField] public Transform       groundCheckPos;
    [SerializeField] public Collider2D      wallCheck;
    [SerializeField] public LayerMask       groundLayer;
    [SerializeField] public Transform       player;
    [SerializeField] public float           chaseRange;
    private float                           distToPlayer;
    private bool                            mustTurn;
    private bool                            mustPatrol;
    //                                      2D collider
    [SerializeField] public Collider2D      bodyCollider;
    //                                      Movement speed
    [SerializeField] public float           m_speed = 4.0f;
    //                                      Rigidbody
    private Rigidbody2D                     m_body2d;
    //                                      Animator (used for changing between animations)
    private Animator                        m_animator;
    //                                      audioSource (used for making sounds)
    private AudioSource                     audioSource;
    //                                      aufio clips
    public AudioClip[]                      hurtClip;
    public AudioClip[]                      deathClip;
    public AudioClip                        attackClip;
    public AudioClip                        whiffClip;



    // Use this for initialization
    void Start () {
        m_animator = GetComponent<Animator>();
        m_body2d = GetComponent<Rigidbody2D>();
        audioSource = gameObject.GetComponent<AudioSource>();


        currentHealth = maxHealth;
        mustPatrol = true;
    }

    private void FixedUpdate()
    {
        if(mustPatrol)
        {
            mustTurn = !Physics2D.OverlapCircle(groundCheckPos.position, 0.1f, groundLayer);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        } else
        {
            m_animator.SetTrigger("Hurt");

            playRandomClip(hurtClip);
        }
    }

    public void Die()
    {
        m_animator.SetTrigger("Death");
        playRandomClip(deathClip);

        bodyCollider.enabled = false;
        StartCoroutine(DestroySelf());
        this.enabled = false;
    }

    private IEnumerator DestroySelf()
    {
        yield return new WaitForSeconds(5);
        Destroy(gameObject);
    }

    // Update is called once per frame
    void Update () {
        if(mustPatrol)
        {
            Patrol();
        }

        distToPlayer = Vector2.Distance(transform.position, player.position);

        if(distToPlayer <= chaseRange)
        {
            ChasePlayer();
        } else
        {
            mustPatrol = true;
        }
    }

    void Attack()
    {
        if (Time.time > next_attack)
        {
            // Detect enemies in range of attack
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, playerLayer);

            if (hitEnemies.Length == 0)
            {
                audioSource.clip = whiffClip;
                audioSource.pitch = UnityEngine.Random.Range(0.8f, 1.1f);
                audioSource.volume = UnityEngine.Random.Range(0.8f, 1);
            }
            else
            {
                audioSource.clip = attackClip;
                audioSource.pitch = UnityEngine.Random.Range(0.8f, 1.1f);
                audioSource.volume = UnityEngine.Random.Range(0.8f, 1);
            }

            // Damage enemies
            foreach (Collider2D enemy in hitEnemies)
            {
                if (enemy.GetComponent<HeroKnight>() != null)
                {
                    m_animator.SetTrigger("Attack");
                    enemy.GetComponent<HeroKnight>().TakeDamage(attackDamage);
                }
            }

            audioSource.Play();

            next_attack = Time.time + attack_cooldown;
        }
    }

    void Patrol()
    {
        m_animator.SetInteger("AnimState", 2);
        if (mustTurn || wallCheck.IsTouchingLayers(groundLayer))
        {
            Flip();
        }

        m_body2d.velocity = new Vector2(m_speed * 30 * Time.fixedDeltaTime, m_body2d.velocity.y);
    }

    void ChasePlayer()
    {
        if (player.position.x > transform.position.x && transform.localScale.x < 0
                || player.position.x < transform.position.x && transform.localScale.x > 0)
        {
            Flip();
        }

        mustPatrol = false;
        m_body2d.velocity = new Vector2(m_speed * 30 * Time.fixedDeltaTime, m_body2d.velocity.y);

        if (distToPlayer <= attackRange * 2 - 0.2f)
        {
            m_animator.SetInteger("AnimState", 1);
            m_body2d.velocity = Vector2.zero;
            Attack();
        }
        else
        {
            m_animator.SetInteger("AnimState", 2);
        }
    }

    void Flip()
    {
        mustPatrol = false;
        transform.localScale = new Vector2(transform.localScale.x * -1, transform.localScale.y);
        m_speed *= -1;
        mustPatrol = true;
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