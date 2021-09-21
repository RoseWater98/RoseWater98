using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public static Enemy enemy;
    public EnemyType EnemyType;
    private Rigidbody2D rb;
    public Transform wallCheck;
    public Transform enemyCheck;
    public Transform groundCheck;
    public LayerMask whatIsWall;
    public LayerMask enemyLayer;
    private LayerMask layers;
    public GameObject spottedImage;
    public StatusEffect StatusEffect;
    public GameObject arrowPrefab;
    public Transform bowPosition;

    [HideInInspector] public int hitDirection;
    private int direction;
    public static int triggerCount = 0;
    public int hurtCount = 4;

    [HideInInspector] public bool lookRight = true;
    private bool detection = false;
    private bool isAttacking = false;
    private bool combo = false;
    private bool death = false;
    private bool onGround = false;
    private bool isTouchingWall = false;
    private bool isTouchingEnemy = false;
    private bool isTouchingGround = false;
    private bool canFlip = true;
    [HideInInspector] public bool afterAttack = false;
    private bool trigger;
    private bool firstDetect = true;
    private bool spotted;

    public float startHurtTime;
    public float startAttackTime;
    public float startComboTime;
    public float walkSpeed;

    private float walkTime = 1.5f;
    private float idleTime = 1f;
    [HideInInspector] public float hurtTime;
    private float attackTime;
    private float comboTime;
    public float actionForce = 10f;
    private float startActionForce;
    public float attackDistance;
    private float startAttackDistance;
    private float nextAttackDistance;
    private float distanceX;
    private float distanceY;
    public float raycastDistance;
    private float wait1Second = 1;
    private float spottedImageTime = 2;


    private Vector2 lookAngle;

    CharacterManager c;
    EnemyManager e;
    Animator anim;
    SpriteRenderer spriteRenderer;

    private void Awake()
    {
        enemy = this;
        layers = LayerMask.GetMask("Ground", "Player"); //raycastin temas edecegi layerlar
    }
    private void Start()
    {
        e = GetComponent<EnemyManager>();
        rb = GetComponent<Rigidbody2D>();
        c = FindObjectOfType<CharacterManager>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        startAttackDistance = attackDistance;
        nextAttackDistance = startAttackDistance + 1;
        startActionForce = actionForce;
        hurtTime = startHurtTime;
        attackTime = startAttackTime;
        comboTime = startComboTime;
        walkTime = 10f / walkSpeed;
        idleTime = 8 / walkSpeed;
        firstDetect = true;
    }
    private void FixedUpdate()
    {
        if (e.hurt && !death) // hasar alirken karakteri geriye ittiren fonksiyonu cagiriyor.
        {
            if (StatusEffect != StatusEffect.Frozen)
            {
                ActionForce();
            }
        }
        if (trigger && !c.isTakingDamage && !death && !c.isDodging && !Knight.airSlamEnd && !Character.airSlamEnd && !Knight.isAttacking && !Character.isAttacking && (Character.onGround || Knight.onGround)) //Karakterin dusmanin icinden gecerken zorlanmasi
        {
            StartCoroutine(TriggerFalse());
            c.GetComponent<Rigidbody2D>().AddForce(-c.transform.right * 300 / triggerCount);
            /* if (distanceX > 0)//dusman sagda
             {
                 c.GetComponent<Rigidbody2D>().AddForce(-Vector2.right * 300);
             }
             else//dusman solda
             {
                 c.GetComponent<Rigidbody2D>().AddForce(Vector2.right * 300);
             }*/
        }
        else
        {
            trigger = false;
        }
    }
    private void Update()
    {
        lookAngle = c.transform.position - transform.position;

        distanceX = rb.transform.position.x - c.transform.position.x;
        distanceY = rb.transform.position.y - c.transform.position.y;

        if (Mathf.Abs(distanceX) < 0.8f && Mathf.Abs(distanceY) < 1) //Karakterin dusmanin icinden gecerken zorlanmasi
        {
            if (firstDetect)
            {
                triggerCount++;
                firstDetect = false;
            }
            trigger = true;
        }
        else
        {
            trigger = false;
            if (!firstDetect)
            {
                triggerCount--;
            }
            firstDetect = true;
        }

        if (rb.transform.position.x < c.transform.position.x) hitDirection = -1; else hitDirection = 1; //karakter dusmanin solundaysa 1 degilse -1

        if (lookRight) direction = 1; else direction = -1; // saga bakiyosam 1, sola -1


        if (StatusEffect != StatusEffect.Frozen)
        {
            CheckWall();
            TakeDamage();
            Attack();
            WalkTrigger();
            Walk();
            Jump();
        }
        Death();
        SpottedImage();
        if (detection && !e.hurt && !isAttacking && !death && c.isAlive && onGround && !afterAttack)
        {
            if (direction == hitDirection)
            {
                Flip();
            }
        }
        if (!c.isAlive && !isAttacking) //ana karakter oluyken dusmanin idle da kalmasi
        {
            anim.speed = 1;
            ParamatersFalse();
            anim.SetBool("idle", true);
        }
        else if (!c.isAlive && isAttacking) //karakteri oldurdukten sonra atak animasyonunu yarida birakmamasi icin animasyonu devam ettiriyor
        {
            AttackFunction();
        }
        if (!combo) attackDistance = startAttackDistance;  // atak mesafesine girince ataktan sonra karakter geri uctugu icin atak mesafesinden geri cikiyor ve combo anime gecemiyo.
        else attackDistance = nextAttackDistance;           // bu yuzden atak mesafesini 1 arttiriyom

        if (hurtCount <= 0) //4. saldiridan sonra hurt calismasin diye
        {
            wait1Second -= Time.deltaTime;
            if (wait1Second < 0)
            {
                hurtCount = 4;
                wait1Second = 1;
            }
        }
    }
    void TakeDamage()
    {
        if (e.Health > 0) //olmediyse
        {
            if (e.hurt) // hasar aldiysa
            {
                if (hurtTime <= 0) // hasar alma suresi bittiyse
                {
                    e.hurt = false;
                    hurtTime = startHurtTime;
                    actionForce = startActionForce;
                }
                else //hasar alirken
                {
                    anim.speed = 1;
                    isAttacking = false;
                    afterAttack = false;
                    combo = false;
                    comboTime = startComboTime;
                    attackTime = startAttackTime;
                    actionForce -= 30 * Time.deltaTime;
                    if (hurtTime == startHurtTime)  // saldiri spamleyince hurt animasyonu sifirlanmiyordu
                    {
                        ParamatersFalse();
                        anim.SetBool("idle", true);
                    }
                    else
                    {
                        ParamatersFalse();
                        anim.SetBool("hurt", true);
                    }
                    hurtTime -= Time.deltaTime;
                }
            }
        }
    }
    void Death()
    {
        if (e.Health <= 0) //olduyse
        {
            anim.speed = 1;
            isAttacking = false;
            combo = false;
            comboTime = startComboTime;
            attackTime = startAttackTime;
            actionForce = startActionForce;
            death = true;
            anim.speed = 1;
            ParamatersFalse();
            anim.SetBool("death", true);
            rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
    }
    void Attack()
    {
        if (detection && !e.hurt && !death && c.isAlive && onGround)
        {
            if (!afterAttack) //atak yapmadan once
            {
                if (Mathf.Abs(distanceX) < attackDistance && Mathf.Abs(distanceY) < 1.5f) // karakterle arasindaki mesafe yakinsa
                {
                    isAttacking = true;
                }
                else
                {
                    if (!isAttacking)
                    {
                        combo = false;
                    }
                }
            }
            else //atak yaptiktan sonra
            {
                isAttacking = false;
                combo = false;
                comboTime = startComboTime;
                attackTime = startAttackTime;
                actionForce = startActionForce;
            }
            AttackFunction();
        }
        else if (!detection && !e.hurt && !death && c.isAlive && onGround) // atak yaparken aramýzdaki mesafe acilirsa animasyonu yarida birakmamasi icin
        {
            AttackFunction();
        }
    }
    void AttackFunction()
    {
        if (isAttacking && !combo) // attack1
        {
            if (attackTime <= 0)
            {
                isAttacking = false;
                if (EnemyType != EnemyType.Archer)
                {
                    combo = true;
                }
                else
                {
                    isAttacking = false;
                    combo = false;
                    afterAttack = true;
                }
                comboTime = startComboTime;
                attackTime = startAttackTime;
                actionForce = startActionForce;
            }
            else
            {
                if (attackTime == startAttackTime)
                {
                    if (direction == hitDirection)
                    {
                        Flip();
                    }
                }
                anim.speed = 1;
                ParamatersFalse();
                anim.SetBool("attack", true);
                attackTime -= Time.deltaTime;
            }
        }

        if (isAttacking && combo) // attack2
        {
            if (comboTime <= 0)
            {
                isAttacking = false;
                combo = false;
                afterAttack = true;
                idleTime = 1.5f;
                comboTime = startComboTime;
                attackTime = startAttackTime;
                actionForce = startActionForce;
            }
            else
            {
                if (comboTime == startComboTime)
                {
                    if (direction == hitDirection)
                    {
                        Flip();
                    }
                }
                anim.speed = 1;
                ParamatersFalse();
                anim.SetBool("attack2", true);
                comboTime -= Time.deltaTime;
            }
        }
        if (!isAttacking && !combo && afterAttack) //attacktan sonra beklemesi
        {
            if (idleTime <= 0) //idle biterse
            {
                canFlip = true;
                if (direction == hitDirection)
                {
                    Flip();
                }
                walkTime = 10f / walkSpeed;
                afterAttack = false;
                idleTime = 8f / walkSpeed;
            }
            else //idle çalýþsýn
            {
                anim.speed = 1;
                ParamatersFalse();
                if (EnemyType == EnemyType.Skeleton3)
                {
                    anim.SetBool("shield", true);
                }
                else
                {
                    anim.SetBool("idle", true);
                }
                idleTime -= Time.deltaTime;
            }
        }
    }
    void WalkTrigger()
    {
        if (detection && !e.hurt && !isAttacking && !death && isTouchingGround && !afterAttack && c.isAlive && onGround && (Mathf.Abs(distanceX) > 2))
        { //karakteri takip etme
            anim.speed = 1.4f;
            walkTime = 0;
            idleTime = 8f / walkSpeed;
            canFlip = false;
            rb.velocity = new Vector2(-walkSpeed * hitDirection * anim.speed, rb.velocity.y);
            ParamatersFalse();
            anim.SetBool("walk", true);
        }
        else if (detection && !e.hurt && !isAttacking && !death && !afterAttack && c.isAlive && onGround /*&& (Mathf.Abs(distanceX) < 2)*/)
        { // karakteri goruyorsa ve arada yukseklik farki varsa karakteri bekliyor
            if (Mathf.Abs(distanceY) > 1.5f || !isTouchingGround)
            {
                anim.speed = 1.4f;
                hurtTime = startHurtTime;
                actionForce = startActionForce;
                walkTime = 0;
                idleTime = 8f / walkSpeed;
                canFlip = false;
                ParamatersFalse();
                anim.SetBool("idle", true);
            }
        }
    }
    void Walk()
    {
        if (!detection && !e.hurt && !isAttacking && !death && !afterAttack && onGround && c.isAlive)
        { //saga sola yurume
            if (walkTime <= 0) // walk biterse
            {
                if (idleTime <= 0) //idle biterse
                {
                    if (canFlip) Flip();
                    canFlip = true;
                    walkTime = 10f / walkSpeed;
                }
                else //idle çalýþsýn
                {
                    anim.speed = 1;
                    ParamatersFalse();
                    anim.SetBool("idle", true);
                    idleTime -= Time.deltaTime;
                }
            }
            else if (isTouchingWall || isTouchingEnemy || !isTouchingGround) // karaktere doðru ilerleyememe
            {
                walkTime = 0;
                idleTime = 8f / walkSpeed;
            }
            else //walk
            {
                idleTime = 8f / walkSpeed;
                rb.velocity = new Vector2(walkSpeed * direction, rb.velocity.y);
                anim.speed = 1;
                ParamatersFalse();
                anim.SetBool("walk", true);
                walkTime -= Time.deltaTime;
            }
        }
    }
    void Jump()
    { // karakter asagi dustugunde
        if (!onGround && !e.hurt)
        {
            isAttacking = false;
            combo = false;
            comboTime = startComboTime;
            attackTime = startAttackTime;
            actionForce = startActionForce;
            anim.speed = 1;
            ParamatersFalse();
            anim.SetBool("jump", true);
        }

    }
    void ActionForce() //karakteri hasar aldiginda geriye ittiren fonksiyon
    {
        if (actionForce > 0) rb.velocity = transform.right * actionForce * hitDirection * direction;
    }
    void Flip()
    {
        if (StatusEffect != StatusEffect.Frozen)
        {
            transform.Rotate(0, 180, 0);
            spottedImage.transform.Rotate(0, 180, 0);
            lookRight = !lookRight;
        }
    }
    private IEnumerator TriggerFalse()
    {
        yield return new WaitForSeconds(0.3f);
        trigger = false;
    }
    private void CheckWall()
    {
        isTouchingWall = Physics2D.Raycast(wallCheck.position, transform.right, 0.5f, whatIsWall); //onundeki duvari kontrol ediyor
        isTouchingEnemy = Physics2D.Raycast(enemyCheck.position, transform.right, 1f, enemyLayer); //onundeki dusmani kontrol ediyor
        isTouchingGround = Physics2D.Raycast(groundCheck.position, -transform.up, 0.5f, whatIsWall); //onundeki zemini kontrol ediyor

        RaycastHit2D los = Physics2D.Raycast(transform.position, lookAngle.normalized, raycastDistance, layers); // karakteri takip eden raycast
        if (los.collider != null)
        {
            if (los.collider.CompareTag("Ground"))
            {
                detection = false;
            }
            if (los.collider.CompareTag("Player"))
            {
                if (CharacterManager.S.CharacterClass == CharacterClass.Knight && Knight.S.KnightActive == KnightActive.Invisibility)
                {
                    detection = false; //dusmanlar bizi goremesin
                }
                else
                {
                    if (!detection && (Mathf.Abs(distanceX) > raycastDistance / 2 && Mathf.Abs(distanceY) < raycastDistance / 2))
                    {
                        spottedImage.SetActive(true);
                        spotted = true;
                    }
                    else if (!detection && Mathf.Abs(distanceY) < raycastDistance)
                    {
                        spottedImage.SetActive(true);
                        spotted = true;
                    }
                    detection = true;
                }
            }
        }
        else
        {
            detection = false;
        }
    }
    private void SpottedImage()
    {
        if (spotted && !death)
        {
            if (spottedImageTime <= 0)
            {
                spottedImage.SetActive(false);
                spotted = false;
                spottedImageTime = 2;
            }
            else
            {
                spottedImageTime -= Time.deltaTime;
            }
        }
    }
    private void OnDrawGizmos() // ustteki raycastleri unity de gormek icin
    {
        Gizmos.DrawLine(wallCheck.position, new Vector3(wallCheck.position.x + 0.5f * direction, wallCheck.position.y, wallCheck.position.z));
        Gizmos.DrawLine(enemyCheck.position, new Vector3(enemyCheck.position.x + 1f * direction, enemyCheck.position.y, enemyCheck.position.z));
        Gizmos.DrawLine(groundCheck.position, new Vector3(groundCheck.position.x, groundCheck.position.y - 0.5f, wallCheck.position.z));
        Gizmos.DrawLine(transform.position, ((Vector2)transform.position + lookAngle.normalized * raycastDistance));
    }
    private void ParamatersFalse() //animasyonlari tek tek false yapan fonksiyon
    {
        for (int i = 0; i < anim.parameters.Length; i++)
        {
            anim.SetBool(anim.parameters[i].name, false);
        }
    }
    public void ThrowArrow()
    {
        Instantiate(arrowPrefab, bowPosition.position, bowPosition.rotation);
        Destroy(GameObject.FindWithTag("Arrow"), 2);
    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.transform.tag == "Ground")
        {
            onGround = true;
        }
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.transform.tag == "Ground")
        {
            onGround = false;
        }
    }

    //STATUS EFFECTLER
    public void Freeze(float duration)
    {
        StartCoroutine(FreezeEnum(duration));
    }
    private IEnumerator FreezeEnum(float duration)
    {
        StatusEffect = StatusEffect.Frozen;
        anim.speed = 0;
        spriteRenderer.color = new Color32(150, 240, 255, 255);
        yield return new WaitForSeconds(duration);
        StatusEffect &= ~StatusEffect.Frozen;
        anim.speed = 1;
        spriteRenderer.color = Color.white;
    }
}
