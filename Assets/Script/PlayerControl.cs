using UnityEngine;
using System.Collections;

public class PlayerControl : MonoBehaviour {
	
	
	private Camera PlayerCam;			// Camera used by the player
	private GameManager _GameManager; 	// GameObject responsible for the management of the game
	private int _activePlayer;
	private bool _player1AI;
	private bool _player2AI;
	private bool _posesion = false;
	
	// Use this for initialization
	void Start () 
	{
		PlayerCam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>(); // Find the Camera's GameObject from its tag 
		_GameManager = gameObject.GetComponent<GameManager>();
		_player1AI = _GameManager.player1AI;
		_player2AI = _GameManager.player2AI;
		
	}
	
	// Update is called once per frame
	void Update () {
		// Look for Mouse Inputs
		_activePlayer = _GameManager.activePlayer;
		if((_activePlayer == 1 && _player1AI == false) || (_activePlayer == -1 && _player2AI == false))
		{
			GetMouseInputs();
		}
	}
	
	/*
	*	@by Rafael Zamora
	*	@modified Mario Acosta
	*/
	// Detect Mouse Inputs
	void GetMouseInputs()
	{	
		_activePlayer = _GameManager.activePlayer;
		Ray _ray;
		RaycastHit _hitInfo;
		// Select a piece if the gameState is 0 or 1
		if(_GameManager.gameState < 4 )
		{
			// On Left Click
			if(Input.GetMouseButtonDown(0))
			{
				_ray = PlayerCam.ScreenPointToRay(Input.mousePosition); // Specify the ray to be casted from the position of the mouse click
				
				// Raycast and verify that it collided
				if(Physics.Raycast (_ray,out _hitInfo))
				{
					if(_GameManager.gameState < 2){
						// Select the piece if it has the good Tag
						if(_hitInfo.collider.gameObject.tag == (_activePlayer.ToString()))
						{
							if(_hitInfo.collider.gameObject.name == "Ball"){
								Debug.Log ("ERROR!");
							}
							_GameManager.SelectPiece(_hitInfo.collider.gameObject);
						}
					}
					else
					{
						Debug.Log ("entro a gamestate 2 o 3");
						if(_hitInfo.collider.gameObject.name == "Ball")
						{
							_GameManager.SelectPiece(_hitInfo.collider.gameObject);
						}
						else
						{
							Debug.Log ("no deberia permitir");

						}
					}
				}
			}
		}
		// Move the piece if the gameState is 1
		if(_GameManager.gameState == 1 || _GameManager.gameState == 3)
		{
			Vector2 selectedCoord;
			
			// On Left Click
			if(Input.GetMouseButtonDown(0))
			{
				_ray = PlayerCam.ScreenPointToRay(Input.mousePosition); // Specify the ray to be casted from the position of the mouse click
				
				// Raycast and verify that it collided
				if(Physics.Raycast (_ray,out _hitInfo))
				{
					
					// If the ray hit a cube, move. If it hit a piece of the other player, eat it.
					if(_hitInfo.collider.gameObject.tag == ("Cube"))
					{
						//Debug.Log ("entro aca en if: " + _hitInfo.collider.gameObject.tag);
						selectedCoord = new Vector2(_hitInfo.collider.gameObject.transform.position.x,_hitInfo.collider.gameObject.transform.position.z);
						
						_GameManager.MovePiece(selectedCoord);
						//TODO: llamar a la funcion posesion de balon y guardar en un bool
							//cambiar turno si perdio la posesion y reinicia el bool de posesion
					}
					else if(_hitInfo.collider.gameObject.tag == ((-1*_activePlayer).ToString()))
					{
//						_GameManager.EatPiece(_hitInfo.collider.gameObject);
						Debug.Log ("intenta comer!!");
					}
				}
			}	
		}
	}
}
