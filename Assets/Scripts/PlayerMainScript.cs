using Cinemachine;
using FishNet.Component.Animating;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.TextCore.Text;

public class PlayerMainScript : MonoBehaviour, ICharacter
{
    public PlayerInputAction playerInputActions;
    [SerializeField] GameObject cam;
    [SerializeField] CinemachineInputProvider inputProvider;
    [SerializeField] GameObject charObj;
    public GameObject CharObj { get { return charObj; } }
    Animator animator;
    NetworkAnimator networkAnimator;

    //Inputs
    public Vector2 moveInput { get; private set; }
    public bool doMove = false;
    public bool doAttack = false;

    CharacterController charController;

    //Stats
    [SerializeField] float moveMultiplier = 1f;
    [SerializeField] float jumpVelocity = 0.5f;
    [SerializeField] float moveRotateLerp = 0.15f;


    [field: SerializeField] public float maxHealth { get; set; } = 100f;
    [field: SerializeField] public float currentHealth { get; set; }
    [field: SerializeField] public float attackPt { get; set; } = 1;
    [field: SerializeField] public float defencePt { get; set; } = 1;

    public enum animations
    {
        IdleWalkRunBlend,
        JumpStart,
        InAir,
        JumpLand,
        Combo1,
        Combo2,
        Combo3
    };

    private void Awake()
    {
        playerInputActions = new PlayerInputAction();
        charController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        networkAnimator = GetComponentInChildren<NetworkAnimator>();
    }

    private void OnEnable()
    {
        playerInputActions.Enable();
    }

    private void OnDisable()
    {
        playerInputActions.Disable();
    }

    private void Start()
    {
        //Application.targetFrameRate = 30;
        currentHealth = maxHealth;
    }

    private void Update()
    {
        moveInput = playerInputActions.Player.Move.ReadValue<Vector2>();

        if (moveInput.magnitude > 0.1f)
        {
            doMove = true;
        }

        if (playerInputActions.Player.Jump.WasPressedThisFrame())
        {
            Jump();
        }
        if (playerInputActions.Player.Fire.WasPressedThisFrame())
        {
            //doAttack = true;
        }
        //Debug.Log(doAttack);
    }

    private void FixedUpdate()
    {
        if (doMove)
        {
            Move(playerInputActions.Player.Move.ReadValue<Vector2>());
            doMove = false;
        }

        charController.SimpleMove(Vector3.zero);

        //if (inAir && grdDetect.IsGrounded && rb.velocity.y <= 0.5f)
        //{
        //    Land();
        //    inAir = false;
        //}
        //animator.SetBool("FreeFall", inAir);
        animator.SetBool("Grounded", charController.isGrounded);
    }

    //Movement

    public void Move(Vector2 input)
    {
        Vector3 direction = (input.x * cam.transform.right + input.y * cam.transform.forward);  //<- somehow normalized doesn't work here
        direction = new Vector3(direction.x, 0, direction.z).normalized;
        charController.SimpleMove(direction * moveMultiplier);

        RotateChar(moveRotateLerp, direction);
        //animator.SetFloat("Speed", Mathf.Lerp(animator.GetFloat("Speed"), input.magnitude, 0.1f));
    }

    public void RotateChar(float lerpValue, Vector3 direction)
    {
        charObj.transform.rotation = Quaternion.Lerp(charObj.transform.rotation, Quaternion.LookRotation(direction), lerpValue);
    }

    void Jump()
    {
        if (charController.isGrounded == false) return;

        //animator.SetTrigger("Jump");
        networkAnimator.SetTrigger("Jump");

        charController.Move(Vector3.up * 1);
    }
    //void Land()
    //{
    //    animator.SetBool("Jump", false);
    //    Debug.Log("Land");
    //}

    //=========Combat

    public void Damage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        Destroy(gameObject);
    }
}
