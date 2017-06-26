﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : Moving
{
    /* Room settings for player */
	IntVector2 currentRoom;
	public Camera mainCamera;
    public Camera miniMapCamera;
    public static Player player;

    /* On event settings */
    Animator animator;
    public int speed = 10;

    // This is used for attacking  by bow. 
    float range = 2f;

    /* UI */
    public Text floorNumber;
	public Text health;
	public Text experienceToNextLevel;

    // Use this for initialization
    void Start ()
    {
		health.text = "Health: " + GameControl.control.playerData.health;
		floorNumber.text = "Floor: " + GameControl.control.playerData.floor;
		experienceToNextLevel.text = "Exp to next level: " + GameControl.control.playerData.getExperienceToNextLevel();

		currentRoom = GameControl.control.startRoom;

		// Set position to one square below center in the start room
		this.transform.SetPositionAndRotation (new Vector3 (
			currentRoom.x * GameConfig.tilesPerRoom + GameConfig.tilesPerRoom/2,// Xpos
			currentRoom.y * GameConfig.tilesPerRoom + GameConfig.tilesPerRoom/2 - 1,// Ypos
			1),// ZPos
			Quaternion.identity);// Rotation

        // Player attack animation 
        base.Awake();
        animator = GetComponent<Animator>();
    }
	
	// Update is called once per frame
	void Update ()
    {
		mainCamera.transform.SetPositionAndRotation (new Vector3 (this.transform.position.x,
			this.transform.position.y, mainCamera.transform.position.z), Quaternion.identity);
        miniMapCamera.transform.SetPositionAndRotation(new Vector3(this.transform.position.x,
            this.transform.position.y, miniMapCamera.transform.position.z), Quaternion.identity);

        // Is player attacking?
        Attack();

        /* Player moving code */
        int x, y;

        x = (int)Input.GetAxisRaw("Horizontal");
        y = (int)Input.GetAxisRaw("Vertical");

        if (x != 0 || y != 0)
        {
            // This prevents diagonal move. 
            if (x != 0)
            {
                y = 0;
            }

            Vector2 start = transform.position;
            Vector2 end = start + new Vector2(x, y);
            Vector2 newPosition = Vector2.MoveTowards(start, end, speed * Time.deltaTime);

            RaycastHit2D hitSword;
            RaycastHit2D hitBow;
			
			// This line is used for testing. 
			// This line is only seen in scene window.
			
            Debug.DrawLine(start, end, Color.red);

			// Below is where attack and hit take place.
			// Now player just stop whenever he is moving toward an enemy within a range
			// or touching it. 
			
           if (IsObstacle (x, y, range, out hitSword, out hitBow)) {

				Debug.Log ("Hit Something");

				Enemy enemyByBow = hitBow.transform.GetComponent<Enemy>();
				// Can attack by bow if the player is using bow
				if (enemyByBow != null){ 
					Debug.Log ("Attack 1");
				}

				Enemy enemyBySword = hitBow.transform.GetComponent<Enemy>();
				// Attack by sword
				if (enemyBySword != null){ 
					Debug.Log ("Attack 2");
				}

				// Detect wall only by raycast and still be a distance from wall
				// so player can move until linecast indicate that player is touching wall.
				Enemy wallOrEnemy = hitBow.transform.GetComponent<Enemy>();

				if (wallOrEnemy == null && hitSword.transform == null) {

					// for debugging
					Debug.Log ("I see wall");
					ToMove (newPosition);
				}
        
			} 
			// if not, just move. 
			else
			{
				Debug.Log (x + "," + y);
				ToMove (newPosition);
			}
        }
    }

    void Attack()
    {
        /* Player attack settings */
        int attackPower = 0;
        bool enemyPresent = false;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            animator.SetTrigger("PlayerAttack");
            if(enemyPresent)
            {
                Enemy.enemy.resetHealth(attackPower);
            }
        }
    }

    public void PlayerGetHit()
    {
        animator.SetTrigger("PlayerHit");
    }
}
