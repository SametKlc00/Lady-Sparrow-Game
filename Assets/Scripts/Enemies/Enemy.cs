using System.Collections;
using UnityEngine;

// tum iskelet dusman davranislarini kontrol eden script

public class Enemy : MonoBehaviour
{
    // rastgele degiskenler
    private Rigidbody2D body;
    public LayerMask playerLayer;
    public Manager manager;
    private Transform playerTransform;

    // ses degiskenleri
    public AudioSource audioSource;
    public AudioClip deathSound;
    public AudioClip hurtSound;

    // hareket degiskenleri
    [SerializeField] private float movementSpeed = 1.5f;
    [SerializeField] private float minChangeDirectionTime = 3f;
    [SerializeField] private float maxChangeDirectionTime = 8f;
    [SerializeField] private float jumpForce = 5f; // yeni ziplama gucu degiskeni
    private Vector2 movementDirection;
    private float directionChangeTimer;

    // saldiri ve can degiskenleri
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1f;
    public Transform enemyAttackBox;
    [SerializeField] private float maxHealth = 30f;
    private float currentHealth;
    public float deathDelay = 1.5f;

    // booleanlar
    public bool isDead = false;
    private bool isEdgeAhead = false;
    public bool isHit;
    public bool isWalking;
    private bool isGrounded = false;
    private bool isFollowingPlayer = false;
    public bool isAttacking = false;

    // animasyon degiskenleri
    public Animator m_Animator;

    // kenar kontrol degiskenleri
    public Transform edgeCheck;  // buraya inspector'dan EdgeCheck objesini surukle

    // duvar kontrol degiskenleri
    public float wallCheckDistance = 0.5f;
    public Transform wallCheck;

    // zemin kontrol degiskenleri
    public Transform groundCheck;  // buraya inspector'dan GroundCheck objesini surukle
    public LayerMask groundLayer;  // inspector'dan Ground Layer'i buraya ata
    private float groundCheckDistance = 0.1f; // raycast'in zemini algilamasi icin mesafe

    void Start() {
        body = GetComponent<Rigidbody2D>();
        movementDirection = Vector2.right; // baslangicta saga hareket
        StartCoroutine(ChangeDirection());
        m_Animator = GetComponent<Animator>();
        manager = GameObject.Find("Manager").GetComponent<Manager>();

        currentHealth = maxHealth;

        // sahnede oyuncu objesi olup olmadigini kontrol et
        GameObject player = GameObject.Find("player");
        if (player != null) {
            playerTransform = player.transform;
        }
    }

    void Update() {
        // duvar kontrolu ve ziplama
        if (IsWallAhead() && isGrounded) {
            Jump();
        }

        // oyuncuyu arama
        if (playerTransform != null) {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= detectionRange) {
                isFollowingPlayer = true;
                if (distanceToPlayer <= attackRange && !isAttacking) {
                    // oyuncu menzildeyse saldir
                    AttackPlayer();
                }
            }
            else {
                isFollowingPlayer = false;
            }
        }
    }

    // oyuncuya saldiri metodu
    void AttackPlayer() {
        if (isAttacking) return;

        Debug.Log("Dusman oyuncuya saldiriyor");
        isAttacking = true;
        m_Animator.SetBool("IsLocked", true);

        m_Animator.SetTrigger("AttackTrigger");
        body.velocity = Vector2.zero;

        StartCoroutine(DelayedDealDamage());
        StartCoroutine(AttackCooldown());
    }

    // saldiri bekleme suresi metodu
    IEnumerator AttackCooldown() {
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
        m_Animator.SetBool("IsLocked", false); // animasyonu kilitten cikar
    }

    // saldiridan bir sure sonra hasar verme (animasyonun ortasinda vurmak icin)
    IEnumerator DelayedDealDamage() {
        yield return new WaitForSeconds(1f); // gerekli sekilde ayarlanabilir

        // oyuncu saldiri kutusunun icinde mi kontrol et
        Collider2D[] hitPlayers = Physics2D.OverlapBoxAll(
            enemyAttackBox.position, 
            enemyAttackBox.localScale, 
            0f, 
            playerLayer
        );

        // oyuncuya hasar ver
        foreach (Collider2D hit in hitPlayers) {
            Player player = hit.GetComponent<Player>();
            if (player != null) {
                player.GetHit();
                Debug.Log("Oyuncu dusman tarafindan vuruldu!");
            }
        }
    }

    void FixedUpdate() {
        isWalking = movementDirection != Vector2.zero;

        // animasyon yurutme parametresini guncelle
        m_Animator.SetBool("IsWalking", isWalking);

        // hareket hizini guncelle
        body.velocity = new Vector2(movementDirection.x * movementSpeed, body.velocity.y);
        
        // yere temas kontrolu
        isGrounded = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);

        // oyuncuya dogru hareket et
        if (isFollowingPlayer && !isAttacking) {
            Vector2 direction = (playerTransform.position - transform.position).normalized;
            body.velocity = new Vector2(direction.x * movementSpeed, body.velocity.y);
            // sprite'i yone gore cevir
            if (direction.x != 0) {
                transform.localScale = new Vector3(Mathf.Sign(direction.x) * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
        }

        // onunde zemin var mi kontrol et
        isEdgeAhead = Physics2D.Raycast(edgeCheck.position, Vector2.down, groundCheckDistance, groundLayer);

        // eger zemin yoksa yon degistir
        if (!isEdgeAhead && isGrounded) {
            movementDirection = -movementDirection;
            Flip((int)movementDirection.x); // sprite'i cevir
        }
    }

    // hareket yonunu degistirme metodu
    private IEnumerator ChangeDirection() {
        while(true) {
            directionChangeTimer = Random.Range(minChangeDirectionTime, maxChangeDirectionTime);
            yield return new WaitForSeconds(directionChangeTimer);

            movementDirection = -movementDirection;
            Flip((int)movementDirection.x);
        }
    }

    // onunde duvar var mi kontrol et
    private bool IsWallAhead() {
        RaycastHit2D hit = Physics2D.Raycast(wallCheck.position, movementDirection, wallCheckDistance, groundLayer);
        return hit.collider != null;
    }

    // ziplama metodu
    private void Jump() {
        body.velocity = new Vector2(body.velocity.x, jumpForce);
    }

    // sprite'i yone gore cevir
    private void Flip(int newDirection) {
        float scaleX = Mathf.Abs(transform.localScale.x) * newDirection;
        transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
    }

    public void TakeDamage(int damage) {
        isHit = true;
        audioSource.PlayOneShot(hurtSound);
        m_Animator.SetTrigger("IsHitTrigger");

        maxHealth -= damage;
        if (maxHealth <= 0 && !isDead) {
            Die();
        }
    }

    // vurulma animasyonunu bitirme metodu
    public void FinishHit() {
        isHit = false;
    }

    // dusman oldugunde calisan metot
    void Die() {
        isDead = true;
        audioSource.PlayOneShot(deathSound);
        m_Animator.SetBool("IsHealth0", true);

        Destroy(gameObject, deathDelay);
        manager.enemiesKilled += 1;
    }

    // sahne editorunde kontrolleri gormek icin
    private void OnDrawGizmos() {
        if (groundCheck != null) {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * groundCheckDistance);
        }
        if (wallCheck != null) {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + Vector3.right * wallCheckDistance);
        }
        if (edgeCheck != null) {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(edgeCheck.position, edgeCheck.position + Vector3.down * groundCheckDistance);
        }
        if (enemyAttackBox != null) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(enemyAttackBox.position, enemyAttackBox.localScale);
        }
    }
}
