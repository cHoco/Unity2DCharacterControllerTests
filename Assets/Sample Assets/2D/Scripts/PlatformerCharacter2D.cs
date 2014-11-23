using UnityEngine;
using System.Collections;

public class PlatformerCharacter2D : MonoBehaviour 
{
	bool facingRight = true;							// For determining which way the player is currently facing.

	[SerializeField] float maxSpeed = 10f;				// The fastest the player can travel in the x axis.
	[SerializeField] float jumpForce = 400f;			// Amount of force added when the player jumps.	

	[Range(0, 1)]
	[SerializeField] float crouchSpeed = .36f;			// Amount of maxSpeed applied to crouching movement. 1 = 100%
	
	[SerializeField] bool airControl = false;			// Whether or not a player can steer while jumping;
	[SerializeField] LayerMask whatIsGround;			// A mask determining what is ground to the character
	
	Transform groundCheck;								// A position marking where to check if the player is grounded.
	float groundedRadius = .2f;							// Radius of the overlap circle to determine if grounded
	bool grounded = false;								// Whether or not the player is grounded.
	Transform ceilingCheck;								// A position marking where to check for ceilings
	float ceilingRadius = .01f;							// Radius of the overlap circle to determine if the player can stand up
	Animator anim;										// Reference to the player's animator component.

	float grabRadius = 6.0f;
	float grabForce = 1000.0f;
	float platformGrabAdditionalMovement = 4.0f;
	float grabMovementSpeed = 25.0f;
	
    void Awake()
	{
		// Setting up references.
		groundCheck = transform.Find("GroundCheck");
		ceilingCheck = transform.Find("CeilingCheck");
		anim = GetComponent<Animator>();
	}

	void OnDrawGizmos() {
		// Visually identify the grab radius from scene
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, grabRadius);
	}

	void Update()
	{
		LayerMask layerMask = 1 << 11;					// User defined layer Grabbable

		// Same code used for the grab mechanic, here used just to debug in scene
		RaycastHit2D[] grabbableObjects = Physics2D.CircleCastAll(transform.position, grabRadius, Vector2.zero, 0.0f, layerMask);
		foreach(RaycastHit2D inRange in grabbableObjects) {
			RaycastHit2D[] hitted = Physics2D.RaycastAll(transform.position, (inRange.point - (Vector2)(transform.position)).normalized, grabRadius, layerMask);
			foreach(RaycastHit2D hittedIn in hitted) {
				Debug.DrawLine(transform.position, hittedIn.point, Color.red);
			}
		}
	}

	void FixedUpdate()
	{

		// The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
		grounded = Physics2D.OverlapCircle(groundCheck.position, groundedRadius, whatIsGround);
		anim.SetBool("Ground", grounded);

		// Set the vertical animation
		anim.SetFloat("vSpeed", rigidbody2D.velocity.y);
	}


	public void Move(float move, bool crouch, bool jump, bool grab)
	{

		// If crouching, check to see if the character can stand up
		if(!crouch && anim.GetBool("Crouch"))
		{
			// If the character has a ceiling preventing them from standing up, keep them crouching
			if( Physics2D.OverlapCircle(ceilingCheck.position, ceilingRadius, whatIsGround))
				crouch = true;
		}

		// Set whether or not the character is crouching in the animator
		anim.SetBool("Crouch", crouch);

		//only control the player if grounded or airControl is turned on
		if(grounded || airControl)
		{
			// Reduce the speed if crouching by the crouchSpeed multiplier
			move = (crouch ? move * crouchSpeed : move);

			// The Speed animator parameter is set to the absolute value of the horizontal input.
			anim.SetFloat("Speed", Mathf.Abs(move));

			// Move the character
			rigidbody2D.velocity = new Vector2(move * maxSpeed, rigidbody2D.velocity.y);
			
			// If the input is moving the player right and the player is facing left...
			if(move > 0 && !facingRight)
				// ... flip the player.
				Flip();
			// Otherwise if the input is moving the player left and the player is facing right...
			else if(move < 0 && facingRight)
				// ... flip the player.
				Flip();
		}

        // If the player should jump...
        if (grounded && jump) {
            // Add a vertical force to the player.
            anim.SetBool("Ground", false);
            rigidbody2D.AddForce(new Vector2(0f, jumpForce));
        }

		// If the player attempts a grab...
		if(grab)
		{
			LayerMask layerMask = 1 << 11; //user defined layer Grabbable
			// To identify grabbable entities we cast a circle
			RaycastHit2D[] grabbableObjects = Physics2D.CircleCastAll(transform.position, grabRadius, Vector2.zero, 0.0f, layerMask);

			// TODO: For each hitted target we remove the ones that aren't front-facing the character and aren't in a specific angle range 
			foreach(RaycastHit2D inRange in grabbableObjects) {
				// For each hitted target we cast another Ray since CircleCast functionn is bugged and the hit points are accurate!
				RaycastHit2D[] hitted = Physics2D.RaycastAll(transform.position, (inRange.point - (Vector2)(transform.position)).normalized, grabRadius, layerMask);

				if ( hitted.Length > 0 ) {
					// TODO: right now only "platform" grabbing implemented, here we should check if the grabbed object is a platform or an enemy and act accordingly

					// At this point we should only have the interesting collider, we define the destination point of the platform grab animation
					Vector2 destination = (Vector2)hitted[0].point + (hitted[0].point - (Vector2)(transform.position)).normalized*platformGrabAdditionalMovement;
					// Start coruotine for the movement animation
					StartCoroutine(GrabMovement(destination));
					//StartCoroutine(GrabMovement(Vector2.Lerp(transform.position, hitted[0].point, 1.5f)));
					/*
					if (grabSpring.enabled) {
						grabSpring.enabled = false;
					}
					else {
						grabSpring.collideConnected = true;
						grabSpring.enabled = true;
						grabSpring.anchor = Vector2.zero;
						grabSpring.connectedBody = null;
						grabSpring.connectedAnchor = hitted[0].point;
						grabSpring.dampingRatio = 1.0f;
						grabSpring.frequency = 2.0f;
						grabSpring.distance = hitted[0].distance/4.0f;
						/*Vector2 forceDirection = (hitted[0].point - (Vector2)transform.position);
					Vector2 appliedForce = new Vector2(forceDirection.x* grabForce, forceDirection.y*grabForce/4);
					Debug.DrawRay(transform.position, forceDirection, Color.green, 10f);
					rigidbody2D.AddForce(appliedForce, ForceMode2D.Force);
					}*/
				}
			}
		}

	}

	IEnumerator GrabMovement(Vector2 target)
	{
		//Vector2 savedVelocity = new Vector2(rigidbody2D.velocity);
		while ((Vector2)transform.position != target)
		{
			// hacky way, better way?
			rigidbody2D.gravityScale = 0;
			transform.position = Vector3.MoveTowards(transform.position, target, grabMovementSpeed * Time.deltaTime);
			yield return 0;
		}

		// hacky way to keep some momentum, should figure out a better solution
		rigidbody2D.velocity = new Vector2(rigidbody2D.velocity.x+10.0f, 0f);
		rigidbody2D.gravityScale = 3;
	}

	void Flip ()
	{
		// Switch the way the player is labelled as facing.
		facingRight = !facingRight;
		
		// Multiply the player's x local scale by -1.
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}
}
