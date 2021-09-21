using UnityEngine;

public class Character : MonoBehaviour
{
    public static Character S; //singleton
    public static bool onGround;
    public static bool airSlam = false;
    public static bool airSlamEnd = false;

    public Transform wallCheck;
    public Transform ledgeCheck;
    public Transform topCheck;
    public LayerMask whatIsGround;

    private Rigidbody2D rb;
    private Transform player;
    private CameraShake cameraShake;

    [HideInInspector] public float horz, direction, horizontalMove, vertDirection;
    private float dodgeTime;
    private float startDodgeTime = 0.6f;
    private float standUpTime;
    private float startStandUpTime = 0.2f;
    private float hurtTime = 0.3f;
    private float hurtTrapTime = 1f;
    private float attackTime = 0.2f;
    private float attackEndTime = 0.2f;
    private float wallSlideSpeed = 2;
    private float wallCheckDistance = 0.5f;
    private float slideToJumpTime = 0.5f;
    private float actionForce = 7;
    private float airAttackTime = 0.4f;
    private float airSlamForceTime = 0.02f;
    private float oneWayTime = 0.4f;

    private bool lookRight = true;
    private bool standUp = false;
    private bool isCrouching = false;
    [HideInInspector] public static bool isAttacking = false;
    [HideInInspector] public static bool oneWayPlatform = false;
    private bool oneWayTrigger = false;
    private bool nextCombo = false;
    private bool afterDeath = false;
    private bool isTouchingWall = false;
    private bool isTouchingLedge = false;
    private bool isTouchingTop = false;
    private bool isWallSliding = false;
    private bool isLedgeClimbing = false;
    private bool slideToJump = false;
    private bool crouchButton = false;
    private bool dodgeButton = false;
    private bool attackButton = false;
    private bool airAttack = false;
    private bool crouched;
    private bool jumpAction;

    private int comboStep;
    private int lookDirection = 1; //1 sað // -1 sol
    private int airSlamForce = -30; //1 sað // -1 sol

    CharacterManager c;
    Dust d;

    [HideInInspector] public InputMaster controls;

    private void Awake()
    {
        //karakter ozelliklerine erisim
        c = FindObjectOfType<CharacterManager>();
        d = FindObjectOfType<Dust>();
        cameraShake = FindObjectOfType<CameraShake>();
        rb = GetComponent<Rigidbody2D>();
        player = GetComponent<Transform>();
        S = this; //singleton

        controls = new InputMaster();   //tuslar

        if (c.isAlive)
        {
            controls.Player.Attack.performed += ctx => AttackButton();
            controls.Player.UseTrinket1.performed += ctx => UseTrinket1();
            controls.Player.UseTrinket2.performed += ctx => UseTrinket2();
            controls.Player.Dodge.performed += ctx => DodgeButton();
            controls.Player.Crouch.performed += ctx => CrouchButton();
            controls.Player.Crouch.canceled += ctx => CrouchButtonExit();
            controls.Player.Jump.performed += ctx => Jump();
            controls.Player.MoveHorizontal.performed += ctx => Movement(ctx.ReadValue<float>());
            controls.Player.MoveVertical.performed += ctx => GamepadCrouch(ctx.ReadValue<float>());
            controls.Player.MoveVertical.canceled += ctx => GamepadCrouchExit();
            controls.Player.SwapTrinketSlots.performed += ctx => c.SwapTrinketSlots();
        }
        standUpTime = startStandUpTime;
        dodgeTime = startDodgeTime;
        attackTime = 0.2f;
        attackEndTime = 0.2f;
        startDodgeTime = 0.6f;
        startStandUpTime = 0.2f;
        hurtTime = 0.3f;
        slideToJumpTime = 0.5f;
        airAttackTime = 0.4f;
    }
    private void Update()
    {
        if (c.isAlive)
        {
            //karakterin yönünü alýp ona göre kuvvet verebilmek için kullanýyom.
            if (lookRight)
            {
                lookDirection = 1;
            }
            else
            {
                lookDirection = -1;
            }

            //slide to jump yaparken zýplamasýn
            if (slideToJump)
            {
                c.JumpCount = 0;
            }
            if (wallSlideSpeed == 20 && !isWallSliding) //slide yaparken aşağı bastıktan sonra yere değince yönünü döndürüyor
            {
                lookRight = !lookRight;
                player.Rotate(0, 180, 0);
                wallSlideSpeed = 2;
            }
            if (!c.isTakingDamage && !isAttacking) //atak yaparken verilen kuvveti ilk deðerine alýyor.
            {
                actionForce = 7;
            }
            if (!isAttacking)
            {
                PlayerAttack.S.isHittingShield = false;
            }
            if (PlayerAttack.S.isHittingShield)
            {
                jumpAction = true;
            }
            //slide to jump yaparken ve saða veya sola basýlý tuttuðumda süresini azaltýyorum
            if (slideToJump && direction != 0)
            {
                // süre bittiðinde de false yapýyorum jump falan çalýþýyor.
                if (slideToJumpTime <= 0)
                {
                    slideToJump = false;
                }
                else
                {
                    slideToJumpTime -= Time.deltaTime;
                }
            }

            if (!c.GodMode) //god mode da hasar almamasý için
            {
                TakeDamage();
                TakeDamageTrap();
            }
            // ledge climb yaptıktan sonra ilk tıklamada dodge çalışmıyordu 2. tıklamada çalışıyordu. O bugu düzeltmek için bu
            if (onGround && !c.isDodging)
            {
                dodgeTime = 0.6f;
            }
            if (!oneWayTrigger) //icinden gectigimiz zemine temas yoksa
            {
                if (oneWayPlatform) // zeminin içinden geçme
                {
                    if (oneWayTime <= 0)
                    {
                        oneWayPlatform = false;
                        oneWayTime = 0.4f;
                    }
                    else
                    {
                        oneWayTime -= Time.deltaTime;
                    }
                }
            }
            //surekli calisanlar
            Fall();
            Flip();
            CheckIfWallSliding();
            CheckIfLedgeClimbing();
            Run();
            Attack();
            AirSlam();
            AirAttack();
            CheckSurroundings();
            if (crouchButton)
            {
                if (onGround || isWallSliding)
                {
                    Crouch();
                }
                else
                {
                    if (c.HasWeapon && !c.isDodging && !c.isTakingDamage && !isLedgeClimbing && !isWallSliding && rb.velocity.y < 4f)
                    {
                        airSlam = true;
                    }
                }
            }
            else StandUp();

            if (dodgeButton)
            {
                Dodge();
            }
            if (attackButton)
            {
                if (c.HasWeapon && !isLedgeClimbing && !isWallSliding)
                {
                    if (onGround)
                    {
                        if (!c.isDodging)
                        {
                            attackButton = false;
                            if (isAttacking) // zaten atak yapýyosam kombo yapsýn
                            {
                                if (!nextCombo) nextCombo = true;
                            }
                            else  // atak yapmýyosam ataðý true yap
                            {
                                isAttacking = true;
                            }
                        }
                        else attackButton = false;
                    }
                    else
                    {
                        if (c.HasWeapon && !c.isDodging)
                        {
                            attackButton = false;
                            airAttack = true;
                            // air slam false yap
                        }
                    }

                }
                else
                {
                    attackButton = false;
                }

            }
            if (onGround)
            {
                if (airSlam)
                {
                    airSlam = false;
                    airSlamEnd = true;
                    d.AirSlamDust();
                }
            }
            //egilme gamepad
            if (vertDirection < 0)
            {
                crouched = true;
                crouchButton = true;
            }
            else
            {
                if (crouched)
                {
                    crouched = false;
                    CrouchButtonExit();
                }
            }
            rb.constraints = RigidbodyConstraints2D.None | RigidbodyConstraints2D.FreezeRotation; //ölü deðilse sabitlemeyi kapatýyor.

            if (rb.velocity.y > 6) //ziplarken zeminlerin icinden gecmek icin trigger yapiyoz
            {
                oneWayPlatform = true;
            }
        }
        else
        {
            //öldükten sonra x ekseninde sabitliyor.
            rb.velocity = new Vector2(0, rb.velocity.y);
            rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        }

        if (afterDeath)
        {
            AfterLife();
        }
    }
    private void FixedUpdate()
    {
        if (c.isAlive)
        {
            if (!isWallSliding && !isLedgeClimbing && !slideToJump && !c.isTakingDamage && !c.isDodging)
            {
                if (direction != 0)
                {
                    rb.velocity = new Vector2(direction * horizontalMove * c.Speed, rb.velocity.y); //sağa ve sola gitme
                }
                else
                {
                    if (horizontalMove == 0 && !onGround)
                    {
                        rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y); //sağa ve sola gitme havadayken
                    }
                    else
                    {

                        //durdugu zaman direction 0 oldugu icin çat diye duruyordu, bu kod onu engelliyor
                        if (lookRight)
                        {
                            rb.velocity = new Vector2(horizontalMove * c.Speed, rb.velocity.y);
                        }
                        else
                        {
                            rb.velocity = new Vector2(-horizontalMove * c.Speed, rb.velocity.y);
                        }

                    }
                }
            }

            //horizontali yavas yavas artirma
            if (direction != 0)
            {
                if (horizontalMove < 0.95f)
                {
                    horizontalMove += 0.05f;
                }
                else
                {
                    horizontalMove = 1;
                }
            }
            else
            {
                if (horizontalMove < 0.05f)
                {
                    horizontalMove = 0;
                }
                else
                {
                    horizontalMove -= 0.05f;
                }
            }
            if (airSlam)
            {
                rb.velocity = new Vector2(0, airSlamForce);
                if (airSlamForceTime <= 0)
                {
                    airSlamForceTime = 0.02f;
                    airSlamForce -= 5;
                }
                else
                {
                    airSlamForceTime -= Time.deltaTime;
                }
            }
            else
            {
                airSlamForceTime = 0.02f;
                airSlamForce = -30;
            }
            //dodge
            if (dodgeTime > 0 && c.isDodging && !isCrouching)
            {
                rb.AddForce(transform.right * 600);
                rb.velocity = new Vector2(0, rb.velocity.y);
            }

            //týrmanýrken aþaðý düþmesin
            if (isLedgeClimbing)
            {
                rb.velocity = new Vector2(0, 0);
                rb.gravityScale = 0;
            }
            else //týrmanma bittiðinde tekrar yer çekimini düzeltiyorum
            {
                rb.gravityScale = 1;
            }

            //slide yaparken karakterin belli hýzda aþaðý kaymasý
            if (isWallSliding && !isLedgeClimbing)
            {
                if (rb.velocity.y < -wallSlideSpeed)
                {
                    rb.velocity = new Vector2(0, -wallSlideSpeed);
                }
            }
            //hasar alýrsa karakteri geriye atmasý
            if (c.isTakingDamage && c.isAlive)
            {
                float a;
                a = EnemyAttack.attacker.transform.position.x;
                if (transform.position.x < a)
                {
                    OnActionMove(-0.4f);
                }
                else if (transform.position.x > a)
                {
                    OnActionMove(0.4f);
                }

            }
            //atak yaparken karakteri ileriye atmasý
            if (isAttacking)
            {
                OnActionMove(0.5f);
                horizontalMove = 0;
            }
            if (jumpAction)
            {
                rb.velocity = new Vector2(-lookDirection * 3, 5);
                isAttacking = false;
                jumpAction = false;
            }
            if (oneWayTrigger) //ziplarken oneWay zeminlerin icinden gecerken yukari dogru kuvvet veriyor
            {
                rb.velocity = new Vector2(rb.velocity.x, 7);
            }
        }
    }
    //idle (veya kosma)
    private void Run()
    {
        if (onGround && !c.isTakingDamage && !airSlamEnd) // yerdeyken çalýþacaklar
        {
            if (!isAttacking) // atak yapmýyosam çalýþsýn
            {
                if (Mathf.Abs(horizontalMove) > 0 && Mathf.Abs(horizontalMove) < 0.6)  //run start
                {
                    AnimationsOfCharacter.S.RunStart();
                    airSlamEnd = false;
                }
                else if (Mathf.Abs(horizontalMove) > 0.6)  //run
                {
                    horz = Mathf.Abs(horizontalMove);
                    AnimationsOfCharacter.S.Run();
                    airSlamEnd = false;
                }
                else
                {
                    if (!standUp && !airSlamEnd) // idle
                    {
                        AnimationsOfCharacter.S.Idle();
                        airSlamEnd = false;
                        horz = 0;
                        isCrouching = false;
                    }
                }
                if (Mathf.Abs(horizontalMove) < Mathf.Abs(horz) && horizontalMove != 0) //run end
                {
                    AnimationsOfCharacter.S.RunEnd();
                    airSlamEnd = false;
                }
            }
        }
    }
    //comelme
    private void Crouch()
    {
        if (!c.isTakingDamage && !isAttacking && !airSlamEnd && !isTouchingTop)
        {
            if (!onGround)
            {
                if (!slideToJump)
                {
                    //slide to fall
                    if (isWallSliding)
                    {
                        wallSlideSpeed = 20;
                    }
                }
            }
            else
            {
                isCrouching = true;
                c.isDodging = false;
                dodgeButton = false;
                dodgeTime = startDodgeTime;
                horizontalMove = 0;
                airSlamEnd = false;
                AnimationsOfCharacter.S.Crouch();
            }
        }
    }
    //ayaga kalkma
    private void StandUp()
    {
        if (standUp && !airSlamEnd && onGround && !isTouchingTop && horizontalMove == 0)
        {
            isCrouching = false;
            AnimationsOfCharacter.S.StandUp();

            if (standUpTime <= 0) //stand up süresi bittiðinde
            {
                standUp = false;
                standUpTime = startStandUpTime; //stand up süresini tekrar sýfýrlýyom
            }
            else
            {
                standUpTime -= Time.deltaTime; // stand up süresi bitmediyse stand up süresini azaltýyom
            }
        }
    }
    private void Jump() // yukardaki duvara degince ziplama calismiyordu. O bugu duzeltmek icin
    {
        if (c.isDodging && !isTouchingTop)
        {
            JumpFunction();
        }
        else if (!c.isDodging)
        {
            JumpFunction();
        }
    }
    //ziplama
    private void JumpFunction()
    {
        //ziplama
        if (c.JumpCount != 0 && !c.isTakingDamage && !isLedgeClimbing && !airSlam && !airSlamEnd) //zýplama hakkým varsa
        {
            if (!isCrouching)
            {
                onGround = false;
                wallSlideSpeed = 2;
                c.JumpCount--;
                comboStep = 0;
                isAttacking = false;
                attackEndTime = 0.2f; // atak animasyonu bitmeden zýplayýnca direk jumpa geçiyor. O yüzden burda atak yapma sürelerini sýfýrlýyoz
                attackTime = 0.2f;
                airSlamEnd = false;
                airAttack = false;
                airAttackTime = 0.4f;

                if (isWallSliding)
                {
                    slideToJump = true;
                }
                if (slideToJump) //slide to jump
                {
                    rb.velocity = new Vector2(-lookDirection * 10, c.JumpHeight);
                    lookRight = !lookRight;
                    player.Rotate(0, 180, 0);
                    d.WallJumpDust();
                }
                else
                {
                    rb.velocity = new Vector2(rb.velocity.x, c.JumpHeight);
                    d.JumpDust();
                }
                AnimationsOfCharacter.S.Jump();
            }
            else // zeminin icinden gecme (egilme ve ziplamaya basma)
            {
                oneWayPlatform = true;
                comboStep = 0;
                isAttacking = false;
                attackEndTime = 0.2f;
                attackTime = 0.2f;
                airSlamEnd = false;
                airAttack = false;
                airAttackTime = 0.4f;
            }
        }
    }
    //dusme
    private void Fall()
    {
        if (!onGround && !c.isTakingDamage && !airSlam)// havadayken çalýþacaklar
        {
            dodgeTime = 0;
            dodgeButton = false;
            c.isDodging = false;
            standUpTime = 0;
            standUp = false;
            if (!isAttacking) // atak yapmýyorsa
            {
                if (rb.velocity.y < -6f) //fall (rb -6 den küçükse)
                {
                    AnimationsOfCharacter.S.Fall();
                    airSlamEnd = false;
                }
                else if (Mathf.Abs(rb.velocity.y) < 6f) // jump to fall(rb 6 ile -6 arasýndaysa)
                {
                    AnimationsOfCharacter.S.JumpToFall();
                    airSlamEnd = false;
                }
            }
        }
    }
    //dodge yapma
    private void Dodge()
    {
        if (!isCrouching && onGround && !airSlamEnd && !c.isTakingDamage) //eðilirken dodge çalýþmasýn, yerdeyken çalýþsýn
        {
            c.isDodging = true;
            isAttacking = false;
            attackEndTime = 0.2f;
            attackTime = 0.2f;
            isLedgeClimbing = false;

            if (c.isDodging)
            {
                if (dodgeTime <= 0) //dodge süresi bittiðinde 
                {
                    AnimationsOfCharacter.S.anim.speed = 1;
                    c.isDodging = false;
                    dodgeButton = false;
                    dodgeTime = startDodgeTime; //dodge yapma süresini tekrar sýfýrlýyom
                }
                else //dodge yap
                {
                    AnimationsOfCharacter.S.Dodge();
                    airSlamEnd = false;
                    if (isTouchingTop && dodgeTime < startDodgeTime * 0.7f)
                    {
                        AnimationsOfCharacter.S.anim.speed = 0;
                    }
                    else
                    {
                        AnimationsOfCharacter.S.anim.speed = 1;
                        dodgeTime -= Time.deltaTime;
                    }

                }
            }
        }
    }

    //atak yapma
    private void Attack()
    {
        //asil atak fonksiyonu
        if (isAttacking && !c.isTakingDamage && !airSlamEnd) // ataðý çaðýrýyoz
        {
            if (comboStep != 0) //1. veya 2.  ataðý þuan yapýyorsa bi sonraki ataða geç
            {
                if (attackTime <= 0) // atak yapma sürem bittiyse
                {
                    if (nextCombo && attackEndTime < c.WeaponSlot.Item.WeaponSpeed) // eðer atak yaparken tekrar atak yapmaya bastýysam bi sonraki saldýrýya geçtirip combo yaptýrýyoruz.
                    {
                        comboStep = 0; //attack 1 yi çalýþtýrmak için
                        attackEndTime = 0.2f;
                        attackTime = 0.2f;
                        actionForce = 7;
                        nextCombo = false;
                    }

                    if (attackEndTime <= 0) // attack end süresi bittiyse false yap
                    {
                        AnimationsOfCharacter.S.AttackAll();
                        attackEndTime = 0.2f;
                        attackTime = 0.2f;
                        comboStep = 0;
                        isAttacking = false;
                        airSlamEnd = false;
                    }
                    else // attack end süresi bitmediyse attack end animasyonunu devam ettir
                    {
                        AnimationsOfCharacter.S.Attack2End();
                        isAttacking = true;
                        isCrouching = false;
                        attackEndTime -= Time.deltaTime;
                        airSlamEnd = false;
                    }
                }
                else // attack yapma sürem bitmediyse ataðý yapmaya devam et
                {
                    AnimationsOfCharacter.S.Attack2();
                    attackTime -= Time.deltaTime;
                    isCrouching = false;
                    standUp = false;
                    standUpTime = startStandUpTime;
                    actionForce -= 30 * Time.deltaTime;
                    StartCoroutine(cameraShake.Shake(.05f, .02f));
                    airSlamEnd = false;
                }
            }
            else // ilk ataðý yaptýr
            {
                if (attackTime <= 0) // atak yapma sürem bittiyse
                {
                    if (nextCombo && attackEndTime < c.WeaponSlot.Item.WeaponSpeed) // eðer atak yaparken tekrar atak yapmaya bastýysam bi sonraki saldýrýya geçtirip combo yaptýrýyoruz.
                    {
                        comboStep = 1; //attack 2 yi çalýþtýrmak için
                        attackEndTime = 0.2f;
                        attackTime = 0.2f;
                        actionForce = 7;
                        nextCombo = false;
                    }

                    if (attackEndTime <= 0) // attack end süresi bittiyse false yap
                    {
                        AnimationsOfCharacter.S.AttackAll();
                        attackEndTime = 0.2f;
                        attackTime = 0.2f;
                        comboStep = 0;
                        isAttacking = false;
                        airSlamEnd = false;
                    }
                    else // attack end süresi bitmediyse attack end animasyonunu devam ettir
                    {
                        AnimationsOfCharacter.S.Attack1End();
                        isAttacking = true;
                        isCrouching = false;
                        standUp = false;
                        standUpTime = startStandUpTime;
                        attackEndTime -= Time.deltaTime;
                        airSlamEnd = false;
                    }
                }
                else // atak yapma sürem bitmediyse ataða devam et
                {
                    AnimationsOfCharacter.S.Attack1();
                    attackTime -= Time.deltaTime;
                    isCrouching = false;
                    standUp = false;
                    standUpTime = startStandUpTime;
                    actionForce -= 30 * Time.deltaTime;
                    StartCoroutine(cameraShake.Shake(.05f, .02f));
                    airSlamEnd = false;
                }
            }
        }
    }
    private void AirAttack()
    {
        if (!c.isTakingDamage && !isLedgeClimbing && !isWallSliding) // ataðý çaðýrýyoz
        {
            if (airAttack)
            {
                if (airAttackTime <= 0)// süre bittiyse
                {
                    airAttack = false;
                    airAttackTime = 0.4f;
                }
                else
                {
                    AnimationsOfCharacter.S.AirAttack();
                    c.isDodging = false;
                    dodgeTime = 0;
                    dodgeButton = false;
                    airAttackTime -= Time.deltaTime;
                }
            }
        }
    }
    //havadan duserek saldirma
    private void AirSlam()
    {
        if (!c.isTakingDamage && !isLedgeClimbing && !isWallSliding) // ataðý çaðýrýyoz
        {
            if (airSlam)
            {
                oneWayPlatform = false;
                AnimationsOfCharacter.S.AirSlam();
                c.isDodging = false;
                dodgeTime = 0;
                dodgeButton = false;
                horizontalMove = 0;
                airAttack = false;
                airAttackTime = 0.4f;
            }
            if (airSlamEnd)
            {
                AnimationsOfCharacter.S.AirSlamEnd();
                StartCoroutine(cameraShake.Shake(.05f, .03f));
                c.isDodging = false;
                dodgeTime = 0;
                dodgeButton = false;
                horizontalMove = 0;
                isAttacking = false;
            }
        }
    }
    private void AirSlamEnd()
    {
        airSlamEnd = false;
        dodgeTime = startDodgeTime;
        AnimationsOfCharacter.S.AirSlamEndFalse();
    }
    //trinket kullanma
    private void UseTrinket1()
    {
        if (c.TrinketSlot.Item != null && c.TrinketSlot.CdCountdown == c.TrinketSlot.MaxCD)
        {
            if (c.TrinketSlot.Item.LimitedUsage && c.TrinketSlot.Ammo != 0)
            {
                c.TrinketSlot.Ammo--;
                c.TrinketSlot.Item.Use(c);
                StartCoroutine(cameraShake.Shake(.05f, .02f));
            }
            else if (!c.TrinketSlot.Item.LimitedUsage)
            {
                c.TrinketSlot.Item.Use(c);
                StartCoroutine(cameraShake.Shake(.05f, .02f));
            }
            c.TrinketSlot.UsedTrinket = true;
        }
    }
    private void UseTrinket2()
    {
        if (c.TrinketSlot2.Item != null && c.TrinketSlot2.CdCountdown == c.TrinketSlot2.MaxCD)
        {
            if (c.TrinketSlot2.Item.LimitedUsage && c.TrinketSlot2.Ammo != 0)
            {
                c.TrinketSlot2.Ammo--;
                c.TrinketSlot2.Item.Use(c);
                StartCoroutine(cameraShake.Shake(.05f, .02f));
            }
            else if (!c.TrinketSlot2.Item.LimitedUsage)
            {
                c.TrinketSlot2.Item.Use(c);
                StartCoroutine(cameraShake.Shake(.05f, .02f));
            }
            c.TrinketSlot2.UsedTrinket = true;
        }
    }
    //parry
    private void SecondaryAttack()
    {
        //burda parry animasyonu calisacak
    }
    //hasar alma
    private void TakeDamage()
    {
        if (c.isTakingDamage && !c.isDodging && !airSlamEnd && !airSlam)
        {
            if (hurtTime <= 0) // hurt yapma sürem bittiyse
            {
                c.isTakingDamage = false;
                hurtTime = 0.3f;
                AnimationsOfCharacter.S.HurtAll();
            }
            else //hurt yapýyorsa
            {
                actionForce -= 30 * Time.deltaTime;
                hurtTime -= Time.deltaTime;
                c.isTakingDamage = true;
                standUpTime = startStandUpTime;
                standUp = false;
                isAttacking = false;
                attackEndTime = 0.2f;
                attackTime = 0.2f;
                comboStep = 0;
                airSlamEnd = false;
                if (c.Health > 0)
                {
                    AnimationsOfCharacter.S.Hurt();
                    StartCoroutine(cameraShake.Shake(.02f, .04f));
                }
                else //öldüyse
                {
                    AnimationsOfCharacter.S.Death();
                    hurtTime = 1f;
                    c.isAlive = false;
                    afterDeath = true;
                }
            }
        }
    }
    //tuzaktan hasar alma
    private void TakeDamageTrap()
    {
        if (c.isTouchingTrap && !c.isDodging && !airSlamEnd && !airSlam)
        {
            if (hurtTrapTime <= 0) // hurt yapma sürem bittiyse
            {
                hurtTrapTime = 1f;
                c.OnTakeDamage(10f);
            }
            else
            {
                if (c.Health > 0)
                {
                    if (hurtTrapTime > 0.8f) //kamera titremesi süresi için (burasý ilerde muhtemelen deðiþir)
                    {
                        StartCoroutine(cameraShake.Shake(.02f, .04f));
                    }
                }
                else //öldüyse
                {
                    hurtTrapTime = 1f;
                    AnimationsOfCharacter.S.Death();
                    c.isAlive = false;
                    afterDeath = true;
                    airSlamEnd = false;
                }
                hurtTrapTime -= Time.deltaTime;
            }
        }
    }
    //kayiyor mu
    private void CheckIfWallSliding()
    {
        if (isTouchingLedge && !isTouchingTop && c.isAlive && !airSlam && !onGround && rb.velocity.y <= 1) //wall slide
        {
            if (!isWallSliding)
            {
                d.WallSlideDust();
            }
            slideToJumpTime = 0.5f;
            isWallSliding = true;
            isLedgeClimbing = false;
            c.JumpCount = 1;
            c.isDodging = false;
            dodgeTime = 0;
            dodgeButton = false;
            airSlamEnd = false;
            airAttack = false;
            airAttackTime = 0.4f;
            AnimationsOfCharacter.S.WallSlide();

        }
        else
        {
            isWallSliding = false;
            AnimationsOfCharacter.S.WallSlideFalse();
        }
    }
    //tutunuyor mu
    private void CheckIfLedgeClimbing()
    {
        if (isTouchingWall && !isTouchingTop && !oneWayPlatform && !isTouchingLedge && c.isAlive && !airSlam && !onGround /*&& rb.velocity.y <= 0*/) //ledge climb
        {
            c.isDodging = false;
            dodgeTime = startDodgeTime;
            dodgeButton = false;
            slideToJumpTime = 0.5f;
            isWallSliding = false;
            isLedgeClimbing = true;
            airSlamEnd = false;
            airAttack = false;
            airAttackTime = 0.4f;
            AnimationsOfCharacter.S.LedgeClimb();
        }
        else
        {
            isLedgeClimbing = false;
            AnimationsOfCharacter.S.LedgeClimbFalse();
        }
    }
    //karakterin baktigi yonu degistirme
    private void Flip()
    {
        if (lookRight == true && direction < 0)
        {
            if (!isWallSliding && !slideToJump && !isLedgeClimbing && !c.isTakingDamage && !c.isDodging)
            {
                lookRight = !lookRight;
                player.Rotate(0, 180, 0);
                horizontalMove = 0;
            }
        }
        else if (lookRight == false && direction > 0)
        {
            if (!isWallSliding && !slideToJump && !isLedgeClimbing && !c.isTakingDamage && !c.isDodging)
            {
                lookRight = !lookRight;
                player.Rotate(0, 180, 0);
                horizontalMove = 0;
            }
        }
    }
    //ledge climb bittikten sonra karakterin pozisyonunu týrmandýðý zemin üzerine alýyor.
    public void ChangePos()
    {
        transform.position = new Vector2(transform.position.x + (lookDirection * 0.2f * transform.localScale.x), transform.position.y + 2f);
        AnimationsOfCharacter.S.LedgeClimbFalse();
        rb.gravityScale = 1f;
        isLedgeClimbing = false;
        dodgeTime = startDodgeTime;
    }
    // unity de karakterin üzerinde görünen 3 tane ýþýn. Duvar algýlama vb. için
    private void CheckSurroundings()
    {
        isTouchingWall = Physics2D.Raycast(wallCheck.position, transform.right, wallCheckDistance, whatIsGround);
        isTouchingLedge = Physics2D.Raycast(ledgeCheck.position, transform.right, wallCheckDistance, whatIsGround);
        isTouchingTop = TopCheck.t.topHit;
    }
    // üstteki raycasti unity içerisinde görmek için
    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(wallCheck.position, new Vector3(wallCheck.position.x + wallCheckDistance * lookDirection, wallCheck.position.y, wallCheck.position.z));
        Gizmos.DrawLine(ledgeCheck.position, new Vector3(ledgeCheck.position.x + wallCheckDistance * lookDirection, ledgeCheck.position.y, ledgeCheck.position.z));
    }
    //karakter oldukten sonra olacaklar
    private void AfterLife()
    {
        standUpTime = startStandUpTime;
        standUp = false;
        hurtTrapTime = 1f;
        c.isTouchingTrap = false;
        c.isTakingDamage = false;
        hurtTrapTime = 0f;
        hurtTime = 0f;
        slideToJumpTime = 0.5f;
        slideToJump = false;
        StartCoroutine(cameraShake.Shake(.05f, .06f));
        rb.gravityScale = 1;
        afterDeath = false;
        airSlamEnd = false;
        isLedgeClimbing = false;
        isWallSliding = false;
        dodgeTime = startDodgeTime;
        attackTime = 0.2f;
        attackEndTime = 0.2f;
        startDodgeTime = 0.6f;
        startStandUpTime = 0.2f;
        hurtTime = 0.3f;
        slideToJumpTime = 0.5f;
        airAttackTime = 0.4f;
    }
    //karakteri hasar alma/verme de ileri ittiren fonksiyon
    public void OnActionMove(float action)
    {
        if (actionForce >= 0)
        {
            if (action == 0.4f)
            {
                rb.velocity = Vector2.right * actionForce * action;
            }
            if (action == -0.4f)
            {
                rb.velocity = -Vector2.left * actionForce * action;
            }
            if (action == 0.5f && !PlayerAttack.S.isHittingShield && !ActionForce.actionForce) //attack
            {
                rb.velocity = transform.right * actionForce * action;
            }
        }
    }

    //COLLISIONLAR
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.transform.tag == "Ground") //zemine degdiginde sifirlanacak seyler
        {
            if (!c.isDodging)
            {
                dodgeTime = startDodgeTime;
                dodgeButton = false;
            }
            standUpTime = startStandUpTime;
            standUp = false;
            attackEndTime = 0.2f;
            attackTime = 0.2f;
            hurtTime = 0.3f;
            slideToJumpTime = 0.5f;
            airAttack = false;
            airAttackTime = 0.4f;
            slideToJump = false;

            //yerdeyse ziplama hakkini ayarlasin
            if (onGround)
            {
                c.JumpCount = 2;
            }
        }
        if (collision.transform.tag == "Trap") //tuzaða deðdiðinde
        {
            if (!c.isDodging)
            {
                if (hurtTrapTime == 1) // önceden hasar almýþsa ve 1 sn geçmemiþse hasar aldýrmýyor
                {
                    hurtTrapTime = 0;
                }
                if (airSlam)
                {
                    airSlam = false;
                    airSlamEnd = true;
                }
                c.isTouchingTrap = true;
            }
        }
    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.transform.tag == "Ground")
        {
            //yerdeyse ziplama hakkini ayarlasin
            if (onGround)
            {
                c.JumpCount = 2;
            }
        }
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.transform.tag == "Trap") //tuzaktan çýktýðýnda
        {
            if (!c.isDodging)
            {
                hurtTrapTime = 0;
                c.isTouchingTrap = false;
            }
        }
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.transform.tag == "Ground" && collision.gameObject.layer == 0)
        {
            if (rb.velocity.y > 6)//ziplarken oneWay zeminlerin icinden geciyorsa yukari dogru kuvvet vermek icin
            {
                oneWayTrigger = true;
            }
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.transform.tag == "Ground" && collision.gameObject.layer == 0)
        {
            oneWayTrigger = false;
        }
    }
    //TUSLAR
    //hareket algilama
    private void Movement(float x)
    {
        if (Mathf.Abs(x) > 0.1)
        {
            Vector2 horzNormalized = new Vector2(x, 0);
            horzNormalized.Normalize();
            direction = horzNormalized.x;
        }
        else
        {
            direction = x = 0;
        }
    }
    //gamepadle oturma
    private void GamepadCrouch(float y)
    {
        if (y < -0.5)
        {
            Vector2 vertNormalized = new Vector2(0, y);
            vertNormalized.Normalize();
            vertDirection = vertNormalized.y;
        }
        else
        {
            vertDirection = y = 0;
        }
    }
    private void GamepadCrouchExit()
    {
        vertDirection = 0;
    }
    //egilme-kalkma tusunu algilama
    private void CrouchButton()
    {
        crouchButton = true;
    }
    private void CrouchButtonExit()
    {
        isCrouching = false;
        crouchButton = false;
        standUp = true;
    }
    //egilme-kalkma tusunu algilama
    private void DodgeButton()
    {
        if (!isCrouching && onGround && !airSlamEnd && !c.isTakingDamage)
        {
            dodgeButton = true;
        }
    }
    //atak tusunu algilama
    private void AttackButton()
    {
        attackButton = true;
    }

    //kontrolleri acip kapatma
    private void OnEnable()
    {
        controls.Enable();
    }
    private void OnDisable()
    {
        controls.Disable();
    }
}
