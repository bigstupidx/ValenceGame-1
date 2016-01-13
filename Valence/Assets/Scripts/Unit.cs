﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Unit : MonoBehaviour {

	public Vector2 currentPosition, lastPosition;
	public ExploreMode_GameController _GameController;
	public GameObject _tile;

	public Vector3 facing;

	//GUI STUFF
	public bool canMove;
	public bool movePressed;
	public bool attackPressed;
	public bool grabPressed;
	public bool waitPressed;
	
	public bool turnComplete;

	/// //////////////////////////////////////////////

	public int actionPoints;
	
	public int health;

	public GameObject myWeapon;

	public int strength;
	public int agility;
	public int perception;

	public int movement;
	public int attackRange;

	public int attackRating;
	public float aimRating;
	
	public bool moving, hasAttacked;
	public Vector2 target;
	
	public float moveSpeed = 1;

	public List<ExploreMode_GameController.Node> currentPath = null;

	public bool isElite;
	public int EliteState;
	public List<Unit> FolkUnits;
	public List<Unit> FolkUnitsWithinView;
	public List<Vector2> knownPosition;

	public Vector2 startPosition;
	public string EliteBehaviour;
	public List<Vector2> PatrolPoints;
	public int currentPatrolPoint;

	public float fieldOfViewAngle = 90;

	public GameObject prefabUnit;
	// Use this for initialization
	void Start () {

		_GameController = GameObject.FindGameObjectWithTag ("GameController").GetComponent<ExploreMode_GameController> ();

		currentPosition = new Vector2( transform.position.x, transform.position.z );
		startPosition = currentPosition;
		moving = false;
		movePressed = false;
		attackPressed = false;
		grabPressed = false;
		waitPressed = false;
		turnComplete = false;
		hasAttacked = false;

		currentPatrolPoint = 0;

		movement = 4 + (agility);
		attackRange = myWeapon.GetComponent<weaponScript> ().range;
		attackRating = myWeapon.GetComponent<weaponScript> ().damageModifier + (strength * 1);
		aimRating = myWeapon.GetComponent<weaponScript> ().accuracy + (perception * 0.1f);


		foreach( Unit fU in _GameController.folk ){
			FolkUnits.Add (fU);
		}
		//_GameController.tiles [(int)currentPosition.x,(int)currentPosition.y] = 3;
	}
	
	// Update is called once per frame
	void Update () {

		if (actionPoints <= 0) {
			turnComplete = true;
		}
		if(currentPath != null) {
			
			int currNode = 0;
			
			while( currNode < currentPath.Count-1 ) {
				//Debug.Log ( "generating line" );
				Vector3 start = new Vector3( currentPath[currNode].x +0.5f, 1, currentPath[currNode].y+0.5f );
				Vector3 end   = new Vector3( currentPath[currNode+1].x+0.5f,1, currentPath[currNode+1].y+0.5f );
				
				currNode++;
			}
			
		}
		MoveNextTile ();
	}
	public void MoveNextTile() {
		float remainingMovement = movement;
		
		while(remainingMovement > 0) {
			if(currentPath==null)
				return;
			
			// Get cost from current tile to next tile
			remainingMovement -= _GameController.CostToEnterTile(currentPath[0].x, currentPath[0].y, currentPath[1].x, currentPath[1].y );

			Vector2 tempFacing = new Vector2( currentPath[1].x, currentPath[1].y ) - currentPosition;
			tempFacing.Normalize();
			facing = new Vector3( tempFacing.x, 0, tempFacing.y );
			//_GameController.tiles [(int)currentPosition.x,(int)currentPosition.y] = 1;
			// Move us to the next tile in the sequence
			currentPosition.x = currentPath[1].x;
			currentPosition.y = currentPath[1].y;
			//_GameController.tiles [(int)currentPosition.x,(int)currentPosition.y] = 3;
			transform.position = new Vector3(currentPosition.x, 0, currentPosition.y);
			
			// Remove the old "current" tile
			currentPath.RemoveAt(0);

			if( isElite ){
				EliteObserve ();
			} else {
				foreach( Unit eU in _GameController.elite ){
					eU.EliteObserve();
				}
			}

			if(currentPath.Count == 1) {
				// We only have one tile left in the path, and that tile MUST be our ultimate
				// destination -- and we are standing on it!
				// So let's just clear our pathfinding info.
				currentPath = null;
				canMove = false;
			}
		} 
		currentPath = null;
		canMove = false;
	}
	
	
	public void Move( Vector2 targetPosition ){
		_GameController.GeneratePathTo ((int)targetPosition.x, (int)targetPosition.y);
	}

	public bool withinMoveRange( Vector2 targetPosition ){
		if (movement >= getDistance (currentPosition, targetPosition)) {
			return true;
		} else {
			return false;
		}
	}
	
	public int getDistance( Vector2 p1, Vector2 p2 ){
		int horD = (int) Mathf.FloorToInt( Vector2.Distance ( new Vector2( p2.x,0), new Vector2( p1.x,0) ) );
		int verD = (int) Mathf.FloorToInt( Vector2.Distance ( new Vector2( 0,p2.y), new Vector2( 0,p1.y) ) );
		int d = horD + verD;
		return d;
	}
	
	void MoveObject(Vector3 currentPos,  Vector3 endPos)
	{
		if (moving) {
			endPos = new Vector3 (endPos.x, 0, endPos.y);
			transform.position = Vector3.Lerp (currentPos, endPos, Time.deltaTime);
			if (transform.position == endPos) {
				moving = false;
			}
		}
	}

	public void EliteObserve(){
		int loopIndex = knownPosition.Count;
		foreach (Unit fU in FolkUnits) {

			if( raycastLineOfSight(fU,false) ){
				if( !FolkUnitsWithinView.Contains (fU) ){
					// ... the player is in sight.
					Debug.Log ("SPOTTED");
					generateSound(currentPosition,3.0f);
					FolkUnitsWithinView.Add (fU);

					if( !knownPosition.Contains ( fU.currentPosition ) )
						knownPosition.Add( fU.currentPosition );
					// Set the last global sighting is the players current position.
					
				} else {
					// Set the last global sighting is the players current position.
					if( !knownPosition.Contains ( fU.currentPosition ) )
						knownPosition.Add( fU.currentPosition );
				} 
			} else {
				if( FolkUnitsWithinView.Contains (fU) )
					FolkUnitsWithinView.Remove (fU);
			}
			loopIndex++;
		}
		for (int i = 0; i < knownPosition.Count; i++) {
			if (currentPosition == knownPosition[i]) {
				knownPosition.Remove( knownPosition[i] );
				i--;
			}
		}


	}

	public void EliteDetermineState(){

		if (knownPosition.Count <= 0) {
			EliteState = 1; // Idle
		} else if (knownPosition.Count > 0 && FolkUnitsWithinView.Count > 0) {
			EliteState = 3; // Attack
		} else if (knownPosition.Count > 0) {
			EliteState = 2; // Search
		}

		switch (EliteState) 
		{
			case 1:
				// conduct default behaviour
				Debug.Log ( "Idle State");
				break;
			case 2:
				Debug.Log ( "Pursuit State");
				break;
			case 3:
				Debug.Log ( "Eliminate State");
				break;
			default:
				Debug.Log ( "No State Defined");
				break;

		}
	}

	public bool raycastLineOfSight( Unit fU , bool ignoreAngle){
		Vector3 direction = fU.transform.position - transform.position;
		//Debug.Log ( direction.normalized.x + " " + direction.normalized.y + " " + direction.normalized.z );
		float angle = Vector3.Angle(direction, facing);
		Debug.DrawLine( fU.transform.position+ new Vector3(0.5f,1.5f,0.5f), transform.position+ new Vector3(0.5f,1.5f,0.5f));
		// If the angle between forward and where the player is, is less than half the angle of view...
		if(angle < fieldOfViewAngle * 0.5f || ignoreAngle == true )
		{
			RaycastHit hit;
			// ... and if a raycast towards the player hits something...
			if(Physics.Raycast(transform.position + new Vector3(0.5f,1.5f,0.5f) , direction.normalized, out hit, 6.0f ) )
			{
				// ... and if the raycast hits the player...
				if(hit.collider.gameObject == fU.gameObject )
				{
					return true;
					/**
					if( !FolkUnitsWithinView.Contains (fU) ){
						// ... the player is in sight.
						Debug.Log ("SPOTTED");

						FolkUnitsWithinView.Add (fU);
						
						// Set the last global sighting is the players current position.
						knownPosition[loopIndex] = fU.currentPosition;
					} else {
						// Set the last global sighting is the players current position.
						knownPosition[loopIndex] = fU.currentPosition;
					} **/
				} else {
					return false;
					//FolkUnitsWithinView.Remove (fU);
				}
			} else {
				return false;
				//if( FolkUnitsWithinView.Contains (fU) )
					//FolkUnitsWithinView.Remove (fU);
			}
		} else {
			return false;
			if( FolkUnitsWithinView.Contains (fU) )
				FolkUnitsWithinView.Remove (fU);
		}
	}

	public Vector2 EliteCalcOptMoveTile(){
		Vector2 targetPosition = currentPosition;
		int myMapSize = _GameController.mapSize;
		if (EliteState == 1) {
			float distToTargetPosition = 99.9f;
			if( currentPosition != startPosition && EliteBehaviour == "Guard" ){
				targetPosition = startPosition;
			} else if ( EliteBehaviour == "Patrol" ){
				if( currentPosition == PatrolPoints[currentPatrolPoint] ){
					currentPatrolPoint++;
					if( currentPatrolPoint >= PatrolPoints.Count ){
						currentPatrolPoint = 0;
					}
				}
				targetPosition = PatrolPoints[currentPatrolPoint];
			}
		}
		else if (EliteState == 2) {
			targetPosition = knownPosition[knownPosition.Count-1];

		} else if (EliteState == 3) {
			GameObject tempUnit = (GameObject) Instantiate ( prefabUnit, this.transform.position, Quaternion.identity );
			Unit tempScript = tempUnit.GetComponent<Unit>();
			tempScript._GameController = GameObject.FindGameObjectWithTag ("GameController").GetComponent<ExploreMode_GameController> ();
			_GameController.selectedUnit = tempScript;
			tempScript.attackRange = attackRange;
			tempScript.currentPosition = currentPosition;
			tempScript.currentPath = null;
			int[,] scores = new int[myMapSize,myMapSize];
			for( int i = (int)currentPosition.x-movement; i < (int)currentPosition.x + movement; i++){
				for( int j = (int) currentPosition.y-movement; j < (int)currentPosition.y + movement; j++){
					if( i >= 0 && i <= myMapSize && j >= 0 && j <= myMapSize ){
						// is it pathable
						if( _GameController.GeneratePathTo(i,j,0)){
							scores[i,j] += 100;
						}
						_GameController.selectedUnit = tempScript;
						tempScript.currentPosition = currentPosition;
						tempScript.currentPath = null;
						if( _GameController.GeneratePathTo( i , j , 0 ) ){
							_GameController.GeneratePathTo( i , j );
							tempScript.MoveNextTile();
							foreach( Unit fU in FolkUnitsWithinView ){
								if( fU.raycastLineOfSight(tempScript,true) && fU.isWithinAttackRange(tempScript) ){
									scores[i,j] -= 10;
								}
								if( tempScript.raycastLineOfSight(fU,false) && tempScript.isWithinAttackRange(fU) ){
									scores[i,j] += 40;
								}
							}
						}
					}
				}
			}
			Destroy ( tempUnit );
			_GameController.selectedUnit = this.gameObject.GetComponent<Unit>();
			int highestScore = 0;
			Vector2 highVector = new Vector2(-1,-1);
			for( int i = (int)currentPosition.x-movement; i < (int)currentPosition.x + movement; i++){
				for( int j = (int) currentPosition.y-movement; j < (int)currentPosition.y + movement; j++){
					if( i >= 0 && i <= myMapSize && j >= 0 && j <= myMapSize ){
						if( scores[i,j] > highestScore ){
							highestScore = scores[i,j];
							highVector = new Vector2(i,j);
						}
					}
				}
			}
			targetPosition = highVector;
		}
//
//		while (_GameController.tiles[(int)targetPosition.x,(int)targetPosition.y] == 2) {
//			List<Vector2> neighbours = new List<Vector2>();
//			if( targetPosition.x > 0 )
//				neighbours.Add ( new Vector2( targetPosition.x-1,targetPosition.y));
//			if( targetPosition.x < myMapSize - 1 )
//				neighbours.Add ( new Vector2(targetPosition.x+1,targetPosition.y) );
//			if( targetPosition.y > 0 )
//				neighbours.Add ( new Vector2(targetPosition.x,targetPosition.y-1) );
//			if( targetPosition.y < myMapSize - 1 )
//				neighbours.Add ( new Vector2(targetPosition.x,targetPosition.y+1) );
//			int numValid = 0;
//			foreach(Vector2 n in neighbours ){
//				if( _GameController.tiles[(int)n.x,(int)n.y] == 1){
//					targetPosition = n;
//					numValid++;
//				}
//			}
//			if( numValid <= 0 ){
//				Vector2 shortestVector = currentPosition;
//				int shortestDist = 99;
//				foreach(Vector2 n in neighbours ){
//					int dist = getDistance ( currentPosition, n );
//					if( shortestDist > dist ){
//						shortestVector = n;
//						shortestDist = dist;
//					}
//				}
//				targetPosition = shortestVector;
//			}
//		}
		return targetPosition;
	}

	public void Attack(Unit targetUnit){
		targetUnit.health -= attackRating;
		Debug.Log ("Attack Dealt " + attackRating + " Damage.");
		Debug.Log ("Unit has " + targetUnit.health + " remaining.");
	}

	public bool isWithinAttackRange(Unit fU ){
		if (!isElite) {
			if (raycastLineOfSight (fU, true) && getDistance (currentPosition, fU.currentPosition) <= attackRange){
				return true;
			} else
				return false;
		} else {
			if (raycastLineOfSight (fU, false) && getDistance (currentPosition, fU.currentPosition) <= attackRange)
				return true;
			else
				return false;
		}
	}

	public void generateSound(Vector2 source, float sndDistance ){
		foreach (Unit eU in _GameController.elite) {
			if( getDistance ( source, eU.currentPosition ) <= sndDistance ){
				if( !eU.knownPosition.Contains(source) )
					eU.knownPosition.Add (source);
			}
		}
	}

	public bool calcChanceToHit(float distance){
		float chance = aimRating - ( 0.1f * distance );

		chance = chance * 100;
		Debug.Log ("Chance to hit: " + chance + "%");
		int rand = Random.Range (0, 100);

		if (rand < chance) {
			Debug.Log("HIT!");
			return true;
		} else {
			Debug.Log("Miss. #sadness #onlyFolkKidsWouldUnderstand");
			return false;
		}
	}
}
