﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    /* Room settings for player */
	IntVector2 currentRoom;
	public Camera mainCamera;
    public Camera miniMapCamera;
    float fullHealth;

	bool isMoving = false;
	Vector3 movementDestination;

    /* On event settings */
    Animator animator;

    /* UI */
	public GameObject displayedStats;
    public Text floorNumber;
	public Text health;
	public Text experienceToNextLevel;
    public Text eventText;
	public Text loadingFloorNumber;
	public Text loadingMilestoneText;
	public GameObject returnToTownPanel;
	public GameObject proceedToNextFloorPanel;
	public GameObject floorLoadingPanel;
	public GameObject gameOverPanel;


    // Use this for initialization
    void Start ()
    {
		StopMovement (true);
		// Pauses the game to display the current floor/milestone for the player
		loadingFloorNumber.text = "Floor number: " + GameControl.control.playerData.floor;
		loadingMilestoneText.text = "Current milestone: " + GameControl.control.playerData.lastMilestone;
		floorLoadingPanel.SetActive (true);

		// Put player in correct start room
		currentRoom = GameControl.control.startRoom;

		// Set position to one square below center in the start room
		this.transform.SetPositionAndRotation (new Vector3 (
			currentRoom.x * GameConfig.tilesPerRoom + GameConfig.tilesPerRoom/2,// Xpos
			currentRoom.y * GameConfig.tilesPerRoom + GameConfig.tilesPerRoom/2 - 1,// Ypos
			1),// ZPos
			Quaternion.identity);// Rotation

		// Add player to turn queue
		GameControl.control.addToTurnQueue (this.name);
		GameControl.control.player = this.gameObject;

        // Player stats
        fullHealth = GameControl.control.playerData.health;
        // Player attack animation 
        animator = GetComponent<Animator>();
    }
	
	// Update is called once per frame
	void Update ()
    {
		// Lock cameras to current position
		mainCamera.transform.SetPositionAndRotation (new Vector3 (this.transform.position.x,
			this.transform.position.y, mainCamera.transform.position.z), Quaternion.identity);
        miniMapCamera.transform.SetPositionAndRotation(new Vector3(this.transform.position.x,
            this.transform.position.y, miniMapCamera.transform.position.z), Quaternion.identity);

        // Update ui info
        health.text = "Health: " + GameControl.control.playerData.health;
		floorNumber.text = "Floor: " + GameControl.control.playerData.floor;
		experienceToNextLevel.text = "Exp to next level: " + GameControl.control.playerData.getExperienceToNextLevel ();

		if (GameControl.control.playerData.health == 0) 
		{
			GameOver ();
		}

        // Keep moving if the player is not at destination yet
        if (isMoving) {
			// If we made it to our desination
			if (Vector3.Distance (this.transform.position, movementDestination) < .01) {
				// Stop moving
				isMoving = false;
			} else {
				// Move along our path
				this.transform.position = Vector3.MoveTowards (this.transform.position, movementDestination, GameConfig.playerMovementSpeed * Time.deltaTime);
			}
		}

		// If it is the player's turn
		if (GameControl.control.isTurn (this.name)) {
            // Is the player trying to move
            eventText.text = "";
			StartMove ();
        }
    }

	void StartMove() {
		/* Player moving code */
		int x, y;

		x = (int)Input.GetAxisRaw ("Horizontal");
		y = (int)Input.GetAxisRaw ("Vertical");

		// If there was some input
		if ((x != 0 || y != 0) && !GameControl.control.stop) {
			// This prevents diagonal move. 
			if (x != 0) {
				y = 0;
			}

			// Set our new desination
			movementDestination = this.transform.position + new Vector3(x, y, 0);

			// See if our desination is valid
			RaycastHit2D hit = Physics2D.Raycast(this.transform.position, new Vector2(x, y));

			// If our rayCast hit something
			if (hit.collider != null) {
				// Useful debug left in for future debugging help
				//Debug.Log("Hit " + hit.transform.name + " at distance " + Vector2.Distance (this.transform.position, hit.transform.position));

				// Check how far our ray went before hitting anything
				if (Vector2.Distance (this.transform.position, hit.transform.position) < 1.51) {// Distance from center of player to collider of object + some error
					// We hit something that we can't move through
					if (hit.transform.tag.Equals ("Shrine")) {
						// Handle shrine interaction
						StopMovement (true);
						returnToTownPanel.SetActive (true);
						return;// Return early to prevent moving into shrine
					} else if (hit.transform.tag.Equals ("Stairs")) {
						// Handle stair interaction
						StopMovement (true);
						proceedToNextFloorPanel.SetActive (true);
						return;// Return early to prevent moving into stairs
					} else if (hit.transform.tag.Equals ("Enemy")) {
						// Do damage to enemy
						int killedEnemy = Attack(hit.transform);
                        if(killedEnemy > 0)
                        {
                            // default gold and exp settings
                            int gold = GameControl.control.playerData.floor + 1;
                            int exp = GameControl.control.playerData.floor + 1;

                            switch (killedEnemy)
                            {
                                case 0:
                                    break;
                                case 1:
                                    gold += 2;
                                    exp += 2;
                                    break;
                                case 2:
                                    gold *= 2;
                                    exp *= 2;
                                    break;
                                case 3:
                                    gold *= 3;
                                    exp *= 3;
                                    break;
                            }
							GameControl.control.playerData.addGold (gold);
							GameControl.control.playerData.addExperience (exp);
                            eventText.text = "Collected " + gold + " coin";
                            // Debug.Log("coin collected");
                        }
                        // Attacking takes up our turn
                        GameControl.control.takeTurn ();
						return;// Return to prevent player from moving into enemy's space
					} else if(hit.transform.tag.Equals("loot")){
                        // get loot bag
                        int bag = hit.transform.GetComponent<Loot>().GetLoot(this.fullHealth);
                        if(bag < 0) //gained health
                        {
                            bag *= -1;
                            eventText.text = "Gained " + bag + " health!";
                        }
                        else
                        {
                            eventText.text = "Collected " + bag + " coin!";
                        }
                        GameControl.control.takeTurn();
                    }
                    else
                    {
                        // Wall or other non-passable object
                        return;// Return so player doesnt move
                    }
				}
			}

			// If we didn't return early, start moving towards destination
			isMoving = true;

			// Mark our turn as over
			GameControl.control.takeTurn ();
		}
	}

	int Attack (Transform enemy)
    {
        /* Player attack settings */
		animator.SetTrigger ("PlayerAttack");
		int damage = GameControl.control.playerData.inventory.getWeapon().getAttackDamage();
		int killedEnemy = enemy.GetComponent<Enemy>().GetHit(damage);
        return killedEnemy;
		//Debug.Log ("Hit enemy for " + damage + " damage!");
    }

	public void PlayHitAnimation ()
    {
		animator.SetTrigger ("PlayerHit");
    }

	public void ContinueAfterFloorLoadingPanel()
	{
		StopMovement (false);
		floorLoadingPanel.SetActive (false);
	}

	public void ReturnToTown ()
	{
        GameControl.control.playerData.health = fullHealth;
		SceneManager.LoadScene ("TownMenu", LoadSceneMode.Single);
	}

	public void DeactivateReturnToTownPanel () 
	{
		StopMovement (false);
		returnToTownPanel.SetActive (false);
	}

	public void ProceedToNextFloor ()
	{
		UpdateStatsOnNewFloor ();
		SceneManager.LoadScene ("Dungeon", LoadSceneMode.Single);
	}

	public void UpdateStatsOnNewFloor ()
	{
		int newFloor = ++GameControl.control.playerData.floor;

		if(newFloor % 2 == 1)
		{
			GameControl.control.playerData.lastMilestone=newFloor;
		}
		GameControl.control.playerData.health = fullHealth;
	}

	public void DeactivateProceedToNextFloorPanel () 
	{
		StopMovement (false);
		proceedToNextFloorPanel.SetActive (false);
	}

	public void ResetToLastMilestone ()
	{
		GameControl.control.playerData.floor = GameControl.control.playerData.lastMilestone;
	}

	public void StopMovement (bool disableMovement)
	{
		GameControl.control.stop = disableMovement;
	}

	public void GameOver ()
	{
		StopMovement (true);
		animator.SetTrigger("PlayerDeath");
		DeactivateStats ();
		LoadGameOverPanel ();
		ResetToLastMilestone ();
	}

	public void DeactivateStats ()
	{
		displayedStats.SetActive (false);
	}

	public void LoadGameOverPanel ()
	{
		gameOverPanel.SetActive (true);
	}

	public void ContinueAfterGameOver ()
	{
		StopMovement (false);
		ReturnToTown ();
	}
}
