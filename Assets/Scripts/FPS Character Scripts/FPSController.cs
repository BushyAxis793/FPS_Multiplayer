using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class FPSController : NetworkBehaviour
{

    private Transform firstPersonView;
    private Transform firstPersonCamera;

    private Vector3 firstPersonViewRotation = Vector3.zero;

    public float walkSpeed = 6.75f;
    public float runSpeed = 10f;
    public float crouchSpeed = 4f;
    public float jumpSpeed = 8f;
    public float gravity = 20f;

    private float speed;

    private bool isMoving, isGrounded, isCrouching;

    private float inputX, inputY;
    private float inputXSet, inputYSet;
    private float inputModifyFactor;

    private bool limitDiagonalSpeed = true;

    private float antiBumpFactor = 0.75f;

    private CharacterController charController;
    private Vector3 moveDirection = Vector3.zero;

    public LayerMask groundLayer;
    private float rayDistance;
    private float defaultControllerHeight;
    private Vector3 defaultCamPos;
    private float camHeight;

    private FPSPlayerAnimations playerAnimation;

    [SerializeField]
    private WeaponManager weaponManager;
    private FPSWeapon currentWeapon;

    private float fireRate = 15f;
    private float nextTimeToFire = 0f;

    [SerializeField]
    private WeaponManager handsWeaponManager;
    private FPSHandsWeapon currentHandsWeapon;

    public GameObject playerHolder, weaponsHolder;
    public GameObject[] weaponsFPS;
    private Camera cam;
    public FPSMouseLook[] mouseLook;

    void Start()
    {
        firstPersonView = transform.Find("FPS View").transform;
        charController = GetComponent<CharacterController>();
        speed = walkSpeed;
        isMoving = false;

        rayDistance = charController.height * 0.5f + charController.radius;
        defaultControllerHeight = charController.height;
        defaultCamPos = firstPersonView.localPosition;

        playerAnimation = GetComponent<FPSPlayerAnimations>();

        weaponManager.weapons[0].SetActive(true);
        currentWeapon = weaponManager.weapons[0].GetComponent<FPSWeapon>();

        handsWeaponManager.weapons[0].SetActive(true);
        currentHandsWeapon = handsWeaponManager.weapons[0].GetComponent<FPSHandsWeapon>();

        if (isLocalPlayer)
        {
            playerHolder.layer = LayerMask.NameToLayer("Player");

            foreach (Transform child in playerHolder.transform)
            {
                child.gameObject.layer = LayerMask.NameToLayer("Player");
            }
            for (int i = 0; i < weaponsFPS.Length; i++)
            {
                weaponsFPS[i].layer = LayerMask.NameToLayer("Player");
            }
            weaponsHolder.layer = LayerMask.NameToLayer("Enemy");

            foreach (Transform child in weaponsHolder.transform)
            {
                child.gameObject.layer = LayerMask.NameToLayer("Enemy");
            }

        }
        if (!isLocalPlayer)
        {
            playerHolder.layer = LayerMask.NameToLayer("Enemy");

            foreach (Transform child in playerHolder.transform)
            {
                child.gameObject.layer = LayerMask.NameToLayer("Enemy");
            }
            for (int i = 0; i < weaponsFPS.Length; i++)
            {
                weaponsFPS[i].layer = LayerMask.NameToLayer("Enemy");
            }
            weaponsHolder.layer = LayerMask.NameToLayer("Player");

            foreach (Transform child in weaponsHolder.transform)
            {
                child.gameObject.layer = LayerMask.NameToLayer("Player");
            }

        }


    }


    void Update()
    {
        if (!isLocalPlayer)
        {
            return;
        }
        PlayerMovement();
        SelectWeapon();
    }

    void PlayerMovement()
    {
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S))
        {
            if (Input.GetKey(KeyCode.W))
            {
                inputYSet = 1f;
            }
            else
            {
                inputYSet = -1f;
            }
        }
        else
        {
            inputYSet = 0f;
        }

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
        {
            if (Input.GetKey(KeyCode.A))
            {
                inputXSet = -1f;
            }
            else
            {
                inputX = 1f;
            }
        }
        else
        {
            inputXSet = 0f;
        }

        inputY = Mathf.Lerp(inputY, inputYSet, Time.deltaTime * 19f);
        inputX = Mathf.Lerp(inputX, inputXSet, Time.deltaTime * 19f);

        inputModifyFactor = Mathf.Lerp(inputModifyFactor,
            (inputYSet != 0 && inputXSet != 0 && limitDiagonalSpeed) ? 0.75f : 1.0f,
            Time.deltaTime * 19f);

        firstPersonViewRotation = Vector3.Lerp(firstPersonViewRotation,
            Vector3.zero, Time.deltaTime * 5f);
        firstPersonView.localEulerAngles = firstPersonViewRotation;

        if (isGrounded)
        {
            PlayerCrouchingAndSprinting();

            moveDirection = new Vector3(inputX * inputModifyFactor, -antiBumpFactor,
                inputY * inputModifyFactor);
            moveDirection = transform.TransformDirection(moveDirection) * speed;

            PlayerJump();
        }
        moveDirection.y -= gravity * Time.deltaTime;

        isGrounded = (charController.Move(moveDirection * Time.deltaTime) & CollisionFlags.Below) != 0;

        isMoving = charController.velocity.magnitude > 0.15f;

        HandleAnimations();

    }

    void PlayerCrouchingAndSprinting()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (!isCrouching)
            {
                isCrouching = true;
            }
            else
            {
                if (CanGetUp())
                {
                    isCrouching = false;
                }
            }

            StopCoroutine(MoveCameraCrouch());
            StartCoroutine(MoveCameraCrouch());
        }

        if (isCrouching)
        {
            speed = crouchSpeed;
        }
        else
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                speed = runSpeed;
            }
            else
            {
                speed = walkSpeed;
            }
        }

        playerAnimation.PlayerCrouch(isCrouching);
    }

    bool CanGetUp()
    {
        Ray groundRay = new Ray(transform.position, transform.up);
        RaycastHit groundHit;

        if (Physics.SphereCast(groundRay, charController.radius + 0.05f, out groundHit, rayDistance, groundLayer))
        {
            if (Vector3.Distance(transform.position, groundHit.point) < 2.3f)
            {
                return false;
            }
        }

        return true;
    }

    IEnumerator MoveCameraCrouch()
    {
        charController.height = isCrouching ? defaultControllerHeight / 1.5f : defaultControllerHeight;
        charController.center = new Vector3(0f, charController.height / 2f, 0f);

        camHeight = isCrouching ? defaultCamPos.y / 1.5f : defaultCamPos.y;


        while (Mathf.Abs(camHeight - firstPersonView.localPosition.y) > 0.01f)
        {
            firstPersonView.localPosition = Vector3.Lerp(firstPersonView.localPosition, new Vector3(defaultCamPos.x, camHeight, defaultCamPos.z), Time.deltaTime * 11f);
            yield return null;
        }
    }

    void PlayerJump()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isCrouching)
            {
                if (CanGetUp())
                {
                    isCrouching = false;

                    playerAnimation.PlayerCrouch(isCrouching);

                    StopCoroutine(MoveCameraCrouch());
                    StartCoroutine(MoveCameraCrouch());
                }
            }
            else
            {
                moveDirection.y = jumpSpeed;
            }
        }
    }

    void HandleAnimations()
    {
        playerAnimation.Movement(charController.velocity.magnitude);
        playerAnimation.PlayerJump(charController.velocity.y);

        if (isCrouching && charController.velocity.magnitude > 0f)
        {
            playerAnimation.PlayerCrouchWalk(charController.velocity.magnitude);
        }

        //shooting
        if (Input.GetMouseButtonDown(0) && Time.time > nextTimeToFire)
        {
            nextTimeToFire = Time.time + 1f / fireRate;

            if (isCrouching)
            {
                playerAnimation.Shoot(false);
            }
            else
            {
                playerAnimation.Shoot(true);
            }
            currentWeapon.Shoot();
            currentHandsWeapon.Shoot();
        }

        if (Input.GetKey(KeyCode.R))
        {
            playerAnimation.Reload();
            currentHandsWeapon.Reload();
        }

    }

    void SelectWeapon()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (!handsWeaponManager.weapons[0].activeInHierarchy)
            {
                for (int i = 0; i < handsWeaponManager.weapons.Length; i++)
                {
                    handsWeaponManager.weapons[i].SetActive(false);
                }

                currentHandsWeapon = null;

                handsWeaponManager.weapons[0].SetActive(true);
                currentHandsWeapon = handsWeaponManager.weapons[0].GetComponent<FPSHandsWeapon>();
            }

            if (!weaponManager.weapons[0].activeInHierarchy)
            {
                for (int i = 0; i < weaponManager.weapons.Length; i++)
                {
                    weaponManager.weapons[i].SetActive(false);
                }

                currentWeapon = null;
                weaponManager.weapons[0].SetActive(true);
                currentWeapon = weaponManager.weapons[0].GetComponent<FPSWeapon>();

                playerAnimation.ChangeController(true);
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (!handsWeaponManager.weapons[1].activeInHierarchy)
            {
                for (int i = 0; i < handsWeaponManager.weapons.Length; i++)
                {
                    handsWeaponManager.weapons[i].SetActive(false);
                }

                currentHandsWeapon = null;

                handsWeaponManager.weapons[1].SetActive(true);
                currentHandsWeapon = handsWeaponManager.weapons[1].GetComponent<FPSHandsWeapon>();
            }

            if (!weaponManager.weapons[1].activeInHierarchy)
            {
                for (int i = 0; i < weaponManager.weapons.Length; i++)
                {
                    weaponManager.weapons[i].SetActive(false);
                }

                currentWeapon = null;
                weaponManager.weapons[1].SetActive(true);
                currentWeapon = weaponManager.weapons[1].GetComponent<FPSWeapon>();

                playerAnimation.ChangeController(false);
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (!handsWeaponManager.weapons[2].activeInHierarchy)
            {
                for (int i = 0; i < handsWeaponManager.weapons.Length; i++)
                {
                    handsWeaponManager.weapons[i].SetActive(false);
                }

                currentHandsWeapon = null;

                handsWeaponManager.weapons[2].SetActive(true);
                currentHandsWeapon = handsWeaponManager.weapons[2].GetComponent<FPSHandsWeapon>();
            }


            if (!weaponManager.weapons[2].activeInHierarchy)
            {
                for (int i = 0; i < weaponManager.weapons.Length; i++)
                {
                    weaponManager.weapons[i].SetActive(false);
                }

                currentWeapon = null;
                weaponManager.weapons[2].SetActive(true);
                currentWeapon = weaponManager.weapons[2].GetComponent<FPSWeapon>();

                playerAnimation.ChangeController(false);
            }
        }
    }









}















































