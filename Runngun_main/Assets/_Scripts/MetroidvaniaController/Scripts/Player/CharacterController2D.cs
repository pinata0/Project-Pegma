using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.SceneManagement;

public class CharacterController2D : MonoBehaviour
{
	[Header("Core Properties")]
	[SerializeField] private bool m_AirControl = true;
	[SerializeField] private LayerMask m_WhatIsGround;
	[SerializeField] private Transform m_GroundCheck;
	[SerializeField] private Transform m_WallCheck;

	const float k_GroundedRadius = .2f; // Radius of the overlap circle to determine if grounded
	private bool m_Grounded;            // Whether or not the player is grounded.
	private Rigidbody2D m_Rigidbody2D;
	private bool m_FacingRight = true;  // For determining which way the player is currently facing.
	private Vector3 velocity = Vector3.zero;

	[Header("Movement Properties")]
	[SerializeField] private float m_JumpForce = 400f;                          // Amount of force added when the player jumps.
	[Range(0, .3f)][SerializeField] private float m_MovementSmoothing = .05f;
	[SerializeField] private float m_DashForce = 25f;
	[SerializeField] private float limitFallSpeed = 25f;
	[SerializeField] private float fallMultiplier = 2.5f;
	[SerializeField] private float lowJumpMultiplier = 2f;
	public bool canDoubleJump = true;

	private bool canDash = true;
	private bool isDashing = false;         //If player is dashing
	private bool m_IsWall = false;          //If there is a wall in front of the player
	private bool isWallSliding = false;     //If player is sliding in a wall
	private bool oldWallSlidding = false;   //If player is sliding in a wall in the previous frame
	private float prevVelocityX = 0f;
	private bool canCheck = false;          //벽 쓸기 상태 확인
	private bool canMove = true;
	private float jumpWallStartX = 0;
	private float jumpWallDistX = 0;            //Distance between player and wall
	private bool limitVelOnWallJump = false;    //For limit wall jump distance with low fps

	[Header("Health Properties")]
	public float life = 10f;
	public bool invincible = false;

	[Header("Particle Properties")]
	private Animator animator;
	[SerializeField] private ParticleSystem particleJumpUp;
	[SerializeField] private ParticleSystem particleJumpDown;
	[SerializeField] private ParticleSystem moveParticleSystem;

	[Header("Events")]
	public UnityEvent OnFallEvent;
	public UnityEvent OnLandEvent;

	[System.Serializable]
	public class BoolEvent : UnityEvent<bool> { }

	private void Awake()
	{
		m_Rigidbody2D = GetComponent<Rigidbody2D>();
		animator = GetComponent<Animator>();

		//OnFallEvent가 null, new UnityEvent()를 사용하여 새로운 UnityEvent 인스턴스를 생성
		if (OnFallEvent == null)
			OnFallEvent = new UnityEvent();

		//OnLandEvent가 null, new UnityEvent()를 사용하여 새로운 UnityEvent 인스턴스를 생성
			OnLandEvent = new UnityEvent();
	}

	private void FixedUpdate()
	{
		bool wasGrounded = m_Grounded;
		m_Grounded = false;

		//만약 땅으로 지정된 어떤 물체에 대한 지면 체크 위치로의 원형 캐스트(circlecast)가 충돌한다면
		//플레이어는 땅에 닿아 있는 것으로 간주됩니다.
		//이를 달성하기 위해 레이어를 사용할 수도 있지만,
		//Sample Assets는 프로젝트 설정을 덮어쓰지 않습니다.
		
		Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
		for (int i = 0; i < colliders.Length; i++)
		{
			if (colliders[i].gameObject != gameObject)
				m_Grounded = true;
			if (!wasGrounded)
			{
				OnLandEvent.Invoke();
				if (!m_IsWall && !isDashing)
					particleJumpDown.Play();
				canDoubleJump = true;
				if (m_Rigidbody2D.velocity.y < 0f)
					limitVelOnWallJump = false;
			}
		}

		m_IsWall = false;

		if (!m_Grounded)
		{
			OnFallEvent.Invoke();
			Collider2D[] collidersWall = Physics2D.OverlapCircleAll(m_WallCheck.position, k_GroundedRadius, m_WhatIsGround);
			for (int i = 0; i < collidersWall.Length; i++)
			{
				if (collidersWall[i].gameObject != null)
				{
					isDashing = false;
					m_IsWall = true;
				}
			}
			prevVelocityX = m_Rigidbody2D.velocity.x;
		}

		if (limitVelOnWallJump)
		{
			if (m_Rigidbody2D.velocity.y < -0.5f)
				limitVelOnWallJump = false;
			jumpWallDistX = (jumpWallStartX - transform.position.x) * transform.localScale.x;
			if (jumpWallDistX < -0.5f && jumpWallDistX > -1f)
			{
				canMove = true;
			}
			else if (jumpWallDistX < -1f && jumpWallDistX >= -2f)
			{
				canMove = true;
				m_Rigidbody2D.velocity = new Vector2(10f * transform.localScale.x, m_Rigidbody2D.velocity.y);
			}
			else if (jumpWallDistX < -2f)
			{
				limitVelOnWallJump = false;
				m_Rigidbody2D.velocity = new Vector2(0, m_Rigidbody2D.velocity.y);
			}
			else if (jumpWallDistX > 0)
			{
				limitVelOnWallJump = false;
				m_Rigidbody2D.velocity = new Vector2(0, m_Rigidbody2D.velocity.y);
			}
		}
	}

	public void Move(float move, bool jump, bool dash)
	{
		//움직일 수 있으면
		if (canMove)
		{
			//이동 파티클 키기
			moveParticleSystem.Play();

			//대쉬상태, 대쉬가능상태, 벽에 부딫힘이 없으면 카운트
			if (dash && canDash && !isWallSliding)
			{
				//m_Rigidbody2D.AddForce(new Vector2(transform.localScale.x * m_DashForce, 0f));
				StartCoroutine(DashCooldown());
			}

			// 앉은 상태면 캐릭터가 일어서는지 체크
			if (isDashing)
			{
				if (transform.rotation.y < 0)
				{
					m_Rigidbody2D.velocity = new Vector2(-transform.localScale.x * m_DashForce, 0);
				}
				else if (transform.rotation.y > 0)
				{
					m_Rigidbody2D.velocity = new Vector2(transform.localScale.x * m_DashForce, 0);
				}
			}
			//땅 위에 있거나 aircontrol이 켜져있으면 제어
			else if (m_Grounded || m_AirControl)
			{
				if (m_Rigidbody2D.velocity.y < -limitFallSpeed)
					m_Rigidbody2D.velocity = new Vector2(m_Rigidbody2D.velocity.x, -limitFallSpeed);
				// Move the character by finding the target velocity
				Vector3 targetVelocity = new Vector2(move * 10f, m_Rigidbody2D.velocity.y);
				// And then smoothing it out and applying it to the character
				m_Rigidbody2D.velocity = Vector3.SmoothDamp(m_Rigidbody2D.velocity, targetVelocity, ref velocity, m_MovementSmoothing);

				//만약 입력이 플레이어를 오른쪽으로 이동시키고 플레이어가 왼쪽을 바라보고 있으면...
				if (move > 0 && !m_FacingRight && !isWallSliding)
				{
					//플레이어 filp
					Flip();
				}
				//만약 입력이 플레이어를 왼쪽으로 이동시키고 플레이어가 오른쪽을 바라보고 있으면...
				else if (move < 0 && m_FacingRight && !isWallSliding)
				{
					//플레이어 filp
					Flip();
				}
			}
			//만약 플레이어가 점프를 해야한다면...
			if (m_Grounded && jump)
			{
				//플레이어에게 수직향력 제공
				animator.SetBool("IsJumping", true);
				animator.SetBool("JumpUp", true);
				m_Grounded = false;
				m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce));
				canDoubleJump = true;
				particleJumpDown.Play();
				particleJumpUp.Play();


				//플레이어를 더 빨리 떨어지게 만들자
				if (m_Rigidbody2D.velocity.y < 0)
				{
					m_Rigidbody2D.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
				}
				else if (m_Rigidbody2D.velocity.y > 0 && !jump)
				{
					m_Rigidbody2D.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
				}


			}
			//더블점프중이고 점프중이며 땅 위나 벽 쓸기 상태가 아닐 때
			else if (!m_Grounded && jump && canDoubleJump && !isWallSliding)
			{
				canDoubleJump = false;
				m_Rigidbody2D.velocity = new Vector2(m_Rigidbody2D.velocity.x, 0);
				m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce / 1.2f));
				animator.SetBool("IsDoubleJumping", true);
			}
			//땅 위가 아니며 벽 앞일때
			else if (m_IsWall && !m_Grounded)
			{
				//직전에 벽 슬라이딩 상태가 아니였으며 아래로 속도가 있을때, 아니면 대쉬중일때
				if (!oldWallSlidding && m_Rigidbody2D.velocity.y < 0 || isDashing)
				{
					//벽 슬라이딩 상태 키기
					isWallSliding = true;
					m_WallCheck.localPosition = new Vector3(-m_WallCheck.localPosition.x, m_WallCheck.localPosition.y, 0);
					Flip();
					StartCoroutine(WaitToCheck(0.1f));
					canDoubleJump = true;
					animator.SetBool("IsWallSliding", true);
				}
				//대쉬 끄기
				isDashing = false;

				//벽 슬라이딩 상태일때
				if (isWallSliding)
				{
					if (move * transform.localScale.x > 0.1f)
					{
						StartCoroutine(WaitToEndSliding());
					}
					else
					{
						oldWallSlidding = true;
						m_Rigidbody2D.velocity = new Vector2(-transform.localScale.x * 2, -5);
					}
				}

				//점프이면서 벽 슬라이싱 상태일때
				if (jump && isWallSliding)
				{

					animator.SetBool("IsJumping", true);
					animator.SetBool("JumpUp", true);
					m_Rigidbody2D.velocity = new Vector2(0f, 0f);
					m_Rigidbody2D.AddForce(new Vector2(transform.localScale.x * m_JumpForce * 1.2f, m_JumpForce));
					jumpWallStartX = transform.position.x;
					limitVelOnWallJump = true;
					canDoubleJump = true;
					isWallSliding = false;
					animator.SetBool("IsWallSliding", false);
					oldWallSlidding = false;
					m_WallCheck.localPosition = new Vector3(Mathf.Abs(m_WallCheck.localPosition.x), m_WallCheck.localPosition.y, 0);
					canMove = false;
				}
				else if (dash && canDash)
				{
					isWallSliding = false;
					animator.SetBool("IsWallSliding", false);
					oldWallSlidding = false;
					m_WallCheck.localPosition = new Vector3(Mathf.Abs(m_WallCheck.localPosition.x), m_WallCheck.localPosition.y, 0);
					canDoubleJump = true;
					StartCoroutine(DashCooldown());
				}
			}
			else if (isWallSliding && !m_IsWall && canCheck)
			{
				isWallSliding = false;
				animator.SetBool("IsWallSliding", false);
				oldWallSlidding = false;
				m_WallCheck.localPosition = new Vector3(Mathf.Abs(m_WallCheck.localPosition.x), m_WallCheck.localPosition.y, 0);
				canDoubleJump = true;
			}
		}
		else
		{
			moveParticleSystem.Stop();
		}
	}

	private void Flip()
	{
		m_FacingRight = !m_FacingRight;
		transform.Rotate(0f, 180f, 0f);
	}

	public void ApplyDamage(float damage, Vector3 position)
	{
		if (!invincible)
		{
			animator.SetBool("Hit", true);
			life -= damage;
			Vector2 damageDir = Vector3.Normalize(transform.position - position) * 40f;
			m_Rigidbody2D.velocity = Vector2.zero;
			m_Rigidbody2D.AddForce(damageDir * 10);
			if (life <= 0)
			{
				StartCoroutine(WaitToDead());
			}
			else
			{
				StartCoroutine(Stun(0.25f));
				StartCoroutine(MakeInvincible(1f));
			}
		}
	}

	IEnumerator DashCooldown()
	{
		animator.SetBool("IsDashing", true);
		isDashing = true;
		canDash = false;
		yield return new WaitForSeconds(0.1f);
		isDashing = false;
		yield return new WaitForSeconds(0.5f);
		canDash = true;
	}

	IEnumerator Stun(float time)
	{
		canMove = false;
		yield return new WaitForSeconds(time);
		canMove = true;
	}
	IEnumerator MakeInvincible(float time)
	{
		invincible = true;
		yield return new WaitForSeconds(time);
		invincible = false;
	}
	IEnumerator WaitToMove(float time)
	{
		canMove = false;
		yield return new WaitForSeconds(time);
		canMove = true;
	}

	IEnumerator WaitToCheck(float time)
	{
		canCheck = false;
		yield return new WaitForSeconds(time);
		canCheck = true;
	}

	IEnumerator WaitToEndSliding()
	{
		yield return new WaitForSeconds(0.1f);
		canDoubleJump = true;
		isWallSliding = false;
		animator.SetBool("IsWallSliding", false);
		oldWallSlidding = false;
		m_WallCheck.localPosition = new Vector3(Mathf.Abs(m_WallCheck.localPosition.x), m_WallCheck.localPosition.y, 0);
	}

	IEnumerator WaitToDead()
	{
		animator.SetBool("IsDead", true);
		canMove = false;
		invincible = true;
		GetComponent<Attack>().enabled = false;
		yield return new WaitForSeconds(0.4f);
		m_Rigidbody2D.velocity = new Vector2(0, m_Rigidbody2D.velocity.y);
		yield return new WaitForSeconds(1.1f);
		SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
	}
}