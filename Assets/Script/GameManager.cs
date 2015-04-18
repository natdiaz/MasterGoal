using UnityEngine;
using System.Collections;

//definiciones que se van a usar
/*
//Tags
#define PLAYERTAG1 1
#define PLAYERTAG2 -1
//La pelota cambia de tag de acuerdo a la posision del balon

//Indexes
//Los indexes de cada pieza depende del jugador que va a jugar (pensando en nivel social)
#define PIECE_INDEX_1 1
#define PIECE_INDEX_2 2
#define PIECE_INDEX_3 3
#define PIECE_INDEX_4 4
#define PIECE_INDEX_5 5

//El index de la pelota no depende del jugador
#define BALL_INDEX 0
*/
static class Constants
{
    //Tags
	public const int PLAYERTAG1 = 1;
	public const int PLAYERTAG2 = -1;
	public const int BALLDEFAULTTAG = 0;
//La pelota cambia de tag de acuerdo a la posision del balon


//Indexes
//Los indexes de cada pieza depende del jugador que va a jugar (pensando en nivel social)
	public const int PIECE_INDEX_1 = 1;
	public const int PIECE_INDEX_2 = 2;
	public const int PIECE_INDEX_3 = 3;
	public const int PIECE_INDEX_4 = 4;
	public const int PIECE_INDEX_5 = 5;

//El index de la pelota no depende del jugador
	public const int BALL_INDEX = 6;
}
public class GameManager : MonoBehaviour {
	
	public GameObject CubeDark;
	public GameObject CubeLight;
	//public GameObjectp[] PiecesGO = new GameObjetcs[2]; 
	public GameObject[] PiecesGO = new GameObject[3]; //pelota: 0 player: 1 keeper: 2

	public int _gamLevel = 1;	//nivel 1 por default
	public int activePlayer = 1;  // 1 = White, -1 = Dark
	
	public int ballMovements = 0; // maximo 4 por turno //TODO: limitar en movepiece
	
	/*
	* In this state, the code is waiting for :
	*	0 = Player selection
	*	1 = Player movement
	*	2 = Ball selection
	*	3 = Ball movement
	*
	*
	*	va de 1 a 0 en caso de no tener posesion
	*	va de 3 a 0 en caso de perder la posesion
	*	va de 3 a 2 en caso de pase (mantener la posesion)
	*
	*	
	*/
	public int gameState = 0;
																	
	public int gameTurn = 0;
	public float turnTime = 0;

	public bool player1AI    = false;  // Bool that state if player1 is a AI
	public bool player2AI    = false;  // Bool that state if player2 is a AI
	public bool debugModeBool = true; //true por default para desarrollo, recuerden poner en false al entregar
	
	public Material DarkMat;
	public Material LightMat;
	public Material[] GrassMat = new Material[2];
	
	public Sprite LineasSpr;


	
	private int _boardHeight = -1;
	private int _pieceHeight =  0;
	
	private GameObject SelectedPiece;	// La pieza seleccionada

//	cambiar de boardSize a fieldSize X or Y
	//private static int _boardSize   =  8;
	private static int _fieldSizeX   =  15;
    private static int _fieldSizeY   =  11;
    /*Matriz logica
    	se accede con [x, 10-y] solo cuando se agarra las coordenadas de la matriz fisica
    	de lo contrario se accede normalmente
    */
    private int[,] _fieldPieces = new int[_fieldSizeX,_fieldSizeY];

	/*TODO: 
		FUNCIONES:
	 	* variable de posesion de balon
		* funcion volver a comenzar
			*pone las piezas en su lugar inicial de acuerdo al nivel


		
	



		ACLARACIONES:
		* el gameState no cambia cuando el player tiene el balon
			el gameState no cambia si en el turno propio movio la pelota a un circulo blanco (casilla especial, se aplica regla de posesion)
			siempre que es 0 cambia de turno
		
		REGLAS DE TABLERO:
		* restringir jugadores para que no entren en el arco
			restringir la pelota para no meter en tu propia area solo si es un pase

		REGLAS DE JUEGO:
		*Portero fuera del area no tiene brazos
		
		*maximo de 4 movimientos (de jugadores, no incluye pelota??) por turno

		*existe el deathlock TODO: averiguar como es
		
		* lista de jugadas ilegales:
			*autopase
			*meter a tu area perdiendo posesion del balon
			*tirar al propio corner
			*ocupar un corner propio
			*la jugada inicial hacia atras
			*no se puede pasar la pelota por encima del arquero cuando esta en su area (el arquero en su area tiene brazos)
			*no se puede pasar la pelota por encima de los defensores dentro del area chica
	 */
	


	void Update () {
		// contar el tiempo
		turnTime += (1 * Time.deltaTime);
	}


	//GUI
	//TODO: agregar mas informacion de la partida para debug
	void OnGUI()
	{
		string _activePlayerColor;
		if(activePlayer == 1)
			_activePlayerColor = "White";
		else
			_activePlayerColor = "Dark";

		//agregar bool para debug
		GUI.Label (new Rect(10,10,200,20), ("Active Player = " + _activePlayerColor));	
		GUI.Label (new Rect(10,20,200,20), ("Ball Possesion = " + BallPossesion()));	

		GUI.Label (new Rect(10,30,200,20), ("Game State = " + gameState));
		GUI.Label (new Rect(10,40,200,20), ("Turn Time = " + ((int)turnTime) ));


		if(debugModeBool)
		{
			GUI.Label (new Rect(10,50,200,20), ("level = " + _gamLevel));
			GUI.Label (new Rect(10,60,200,20), ("turno = " + gameTurn));
			/*TODO:datos para debbug
				datos de la partida
					*state
					*turno
					*posicion de la pieza seleccionada
					*posicion de la pelota	
			*/
			GUI.Label (new Rect(10, 70, 200, 20), ("DEBBUG MODE BOARD:"));//modificar la posision
			for(int i = 0; i < _fieldSizeX; i++)
			{
				for(int j = 0; j < _fieldSizeY; j++)
				{
					if((i==0 || i==14) && (j<3 || j>7) ) //entre 0,0 al 0,2 o 0,7 al 0,10
					{
	
					}else{
						GUI.Label (new Rect(10+15*i, (90+10*j), 200, 20), (" "+_fieldPieces[i,j]));//modificar la posision
					}
				}
			}
		}
	}


	// Initialize field
	void Start()
	{
		CreateField();
		AddBall();
		AddPieces();
	}



    /*
    *	@CreateField
    *	Crea los bloques y los ordena en forma de cancha
    *	Inserta un Sprite y cambia la rotacion, posicion y escala para ajustarlo a la cancha
	*
	*	@by: Rafael Zamora
    */
	void CreateField()
	{
		// Crea cada bloque y ponerlos para formar la cancha
		for(int i = 0; i < _fieldSizeX; i++)
		{
			for(int j = 0; j < _fieldSizeY; j++)
			{
				// Para crear los arcos
				if((i==0 || i==14) && (j<3 || j>7) ) // entre 0,0 al 0,2 o 0,7 al 0,10
				{										// y del 14,0 al 14,2 o 14,7 al 14,10
					// No crea nada
				}	
				else{	// Crea los bloques del tablero
					if((i+j)%2 == 0)
					{
						Object.Instantiate(CubeDark,new Vector3(i,_boardHeight,j), Quaternion.identity);	
					}
					else
					{
						Object.Instantiate(CubeLight,new Vector3(i,_boardHeight,j), Quaternion.identity);
					}
				}
			}
		}
		// Insertar el sprite de las lineas de la cancha
        // crea el objeto
        GameObject lineasGO = new GameObject();
        lineasGO.name = "Lineas";
        // Agrega el componente "SpriteRenderer" al gameobject
        lineasGO.AddComponent<SpriteRenderer>();
        // Asigna el sprite
        lineasGO.GetComponent<SpriteRenderer>().sprite = LineasSpr;
        // Modifica la rotacion, la posicion y la escala
        lineasGO.transform.Rotate(90, 0, 0);
        lineasGO.transform.position = new Vector3(7.025f,-0.486f,5.0f);
        lineasGO.transform.localScale = new Vector3(0.832f,0.832f,0.83f);

	}


   /*
    *	@AddBall()
 	*   Llama a @CreateBall() y a @InitPosBall()
	*	
	*	@by: Rafael Zamora
    */
	void AddBall()
	{
		CreateBall(); //(x,y)
		initPosBall();
	}
	


	/*
    *	@CreateBall()
 	*   
	*
	*	@by: Rafael Zamora
    */
	void CreateBall()
	{
		GameObject _PieceToCreate = null;
		int 	   _pieceIndex = Constants.BALL_INDEX;
		int _posX = 7;
		int _posY = 5;
		
		_PieceToCreate = PiecesGO[_pieceIndex];
		
		// Instantiate the ball as a GameObject to be able to modify it after
		_PieceToCreate = Object.Instantiate (_PieceToCreate, new Vector3(_posX, _pieceHeight, _posY), Quaternion.identity) as GameObject; //TODO:posicion correcta en altura de la pelota
		_PieceToCreate.name = "Ball";
		_PieceToCreate.tag = "0";
		_PieceToCreate.GetComponent<Renderer>().material.color = Color.yellow;
		
		//agrega a la matriz tablero
		_fieldPieces[_posX,10-_posY] = 6;
	}

	/*
    *	@initPosBall()
 	*   
	*
	*	@by: Rafael Zamora
    */
	void initPosBall()
	{
		int _initPosX = 7;
		int _initPosY = 5;
		Vector2 initCoords = new Vector2(7.0f,5.0f);
		Vector2 _currentCoor;


		GameObject _PieceToMove = null;
		//Debug.Log ("initPosBall");
		//initCoords.Set;

		_PieceToMove = GameObject.Find("Ball");
		//Debug.Log ("_PieceToMove " + _PieceToMove);

		//guardar la posicion actual para cerar en el tablero
		_currentCoor = new Vector2(_PieceToMove.transform.position.x, _PieceToMove.transform.position.z);//z para el vector de 3D
		_fieldPieces[(int)_currentCoor.x,10-(int)_currentCoor.y] = 0;

		//mover la pieza y guardar en el tablero
		_PieceToMove.transform.position = new Vector3(_initPosX, _pieceHeight, _initPosY);		// Move the piece
		_fieldPieces[_initPosX,10-_initPosY] = 6;	
	}


	/*
    *	@AddPieces()
 	*   
	*
	*	@by: Rafael Zamora
	*	//TODO: hacer para el level 3 con el arquero
    */
	void AddPieces()
	{
		if(_gamLevel==1)
		{
			CreatePiece("Player", 3, 5, Constants.PLAYERTAG1);	
			CreatePiece("Player", 11, 5, Constants.PLAYERTAG2);
		}else if(_gamLevel==2)
		{
			CreatePiece("Player", 2, 5, Constants.PLAYERTAG1);	
		    CreatePiece("Player", 2, 6, Constants.PLAYERTAG1);	
			CreatePiece("Player", 10, 5, Constants.PLAYERTAG2);
			CreatePiece("Player", 10, 6, Constants.PLAYERTAG2);
		}else if(_gamLevel==3){
	
		}else //TODO: hacer para el level 3 con el arquero
		{
			Debug.Log ("no existe este level");
		}
		initPieces();
	}

	/*
    *	@CreatePiece(string _pieceName, int _posX, int _posY, int _playerTag)	
 	*   @param _pieceName: nombre de la pieza
	*


	*	@by: Rafael Zamora
	*	//TODO: modificar el 7 para piecesGO
    */
	void CreatePiece(string _pieceName, int _posX, int _posY, int _playerTag)	
	{
		GameObject _PieceToCreate = null;
		int 	   _pieceIndex = 0;
		//index por nivel 1*_player
		if(_pieceName=="Player"){
			_pieceIndex = 1;			
		}else if(_pieceName=="Keeper"){
			_pieceIndex = 2;			
		}
		_PieceToCreate = PiecesGO[7-1];//TODO: modificar el 7

		// Instantiate the piece as a GameObject to be able to modify it after
		_PieceToCreate = Object.Instantiate (_PieceToCreate, new Vector3(_posX, _pieceHeight, _posY), Quaternion.identity) as GameObject;
		_PieceToCreate.name = _pieceName;
		if(_playerTag == 1)
		{
			_PieceToCreate.tag = "1";
			_PieceToCreate.GetComponent<Renderer>().material.color = Color.white;
		}
		else if(_playerTag == -1)
		{
			_PieceToCreate.tag = "-1";
			_PieceToCreate.GetComponent<Renderer>().material.color = Color.red;		
		}
		//Agrega a la matriz tablero
		_fieldPieces[_posX,10-_posY] = _pieceIndex*_playerTag;
	}



    /*
    *	@initPieces
    *	Busca todos las piezas con tag de cada player
    *	Para cada pieza:
    *			Guarda la coordenada actual del tablero fisico
    *			Cera su posision en el tablero logico
    *			Mueve la pieza a la posicion que le corresponde
	*
	*	@by: Rafael Zamora
	*	TODO:
	*		Terminar el resto de las coords para demas niveles(para el keeper)
	*		Hacer en un solo bloque
    */
	void initPieces()
    {
	GameObject _PieceToMove = null;
    GameObject[] Pieces;
    Vector2 _currentCoor;
    Vector2[] initCoords = new Vector2[5];
    int i;
    //Asigna las coordenadas iniciales
    switch (_gamLevel)
    {
    	case 1:
    		initCoords[0].Set(4.0f,5.0f);
    		break;
     	case 2:
     		initCoords[0].Set(4.0f,5.0f);
    		initCoords[1].Set(4.0f,5.0f);
    		break;
    	case 3:
    		break;	
    	default:
    		break;
	}
    //primero mover los players

    //llamar a todos los jugadores de un equipo
        Pieces = GameObject.FindGameObjectsWithTag("1");
        
        foreach (GameObject Piece in Pieces) {
        	_PieceToMove = Piece;

        	//TODO: if .name == "Keeper" poner en su posicion
        	//guardar la posicion actual para cerar en el tablero
			_currentCoor = new Vector2(_PieceToMove.transform.position.x, _PieceToMove.transform.position.z);//z para el vector de 3D
			_fieldPieces[(int)_currentCoor.x, (10-(int)_currentCoor.y)] = 0;

			//mover la pieza y guardar en el tablero
			_PieceToMove.transform.position = new Vector3(initCoords[0].x, _pieceHeight, initCoords[0].y);// Move the piece
			_fieldPieces[((int)initCoords[0].x),(10-(int)initCoords[0].y)] = 1;
        }
    
    //llamar a todos los jugadores del otro equipo
            Pieces = GameObject.FindGameObjectsWithTag("-1");
        
        foreach (GameObject Piece in Pieces) {
        	_PieceToMove = Piece;

        	//TODO: if .name == "Keeper" poner en su posicion
        	//guardar la posicion actual para cerar en el tablero
			_currentCoor = new Vector2(_PieceToMove.transform.position.x, _PieceToMove.transform.position.z);//z para el vector de 3D
			_fieldPieces[(int)_currentCoor.x, (10-(int)_currentCoor.y)] = 0;

			//mover la pieza y guardar en el tablero
			_PieceToMove.transform.position = new Vector3((14-initCoords[0].x), _pieceHeight, initCoords[0].y);// Move the piece
			_fieldPieces[(14-(int)initCoords[0].x),(10-(int)initCoords[0].y)] = -1;
        }
    }




    /*
    *	@ReturnColorToPiece
    *	Pinta con su color original las piezas al dejar de estar seleccionadas
    *	Si es pelota:
    *			Amarillo
    *	Si es jugador 1:
    *			Blanco
    *	Si es jugador 2:
    *			Rojo
	*	@by: Rafael Zamora
    */
	public void ReturnColorToPiece()
	{
		if(SelectedPiece.name == "Ball")//name pelota
		{
			SelectedPiece.GetComponent<Renderer>().material.color = Color.yellow;
		}else{
			if(SelectedPiece.tag == "1") //tag jugador 1
			{
				SelectedPiece.GetComponent<Renderer>().material.color = Color.white;
			}else //tag jugador 2
			{
				SelectedPiece.GetComponent<Renderer>().material.color = Color.red;
			}
		}
	}


    /*
    *	@SelectPiece
    *	Pinta con su color original las piezas al dejar de estar seleccionadas
    *	Si es pelota:
    *			Amarillo
    *	Si es jugador 1:
    *			Blanco
    *	Si es jugador 2:
    *			Rojo
	*	@by: Rafael Zamora
	*	@modified: Mario Acosta
    */
	public void SelectPiece(GameObject _PieceToSelect)
	{
		Debug.Log ("SelectPiece");
		//TODO: solo seleccionar pelota si tenes posesion
		// Unselect the piece if it was already selected
		if(_PieceToSelect.name !="Ball")
		{
			if(_PieceToSelect  == SelectedPiece)
			{
				if (gameState == 1){
					ReturnColorToPiece();
					SelectedPiece = null;
					ChangeState (0);				
				}
			}
			else
			{
				//Debug.Log ("no_seleccionada tag: " + _PieceToSelect.tag);
				// Change color of the selected piece to make it apparent. Put it back to white when the piece is unselected
				if(SelectedPiece)
				{
					ReturnColorToPiece();
				}
				SelectedPiece = _PieceToSelect;
				SelectedPiece.GetComponent<Renderer>().material.color = Color.blue;
				ChangeState (1);
			}
		}
		else
		{
			SelectedPiece = _PieceToSelect;
			SelectedPiece.GetComponent<Renderer>().material.color = Color.blue;
			ChangeState (3);
		}
	}


	//FUNCION MODIFICADA
	// Move the SelectedPiece to the inputted coords
	//Mueve la pieza TODO: Revisar si la pieza es pelota
	public void MovePiece(Vector2 _coordToMove)
	{
		Debug.Log ("MovePiece");
		bool validMovementBool = false;
		Vector2 _coordPiece = new Vector2(SelectedPiece.transform.position.x, SelectedPiece.transform.position.z);
		
		// Don't move if the user clicked on its own cube or if there is a piece on the cube
		//TODO: or if they ar goalkeepers arms on the cubes inside the goalkeep
		if((_coordToMove.x != _coordPiece.x || _coordToMove.y != _coordPiece.y) || _fieldPieces[(int)_coordToMove.x,10-(int)_coordToMove.y] != 0)
		{
			validMovementBool	= TestMovement (SelectedPiece, _coordToMove);
		}
		
		//Debug.Log ("MovePiece x: " + ((int)_coordToMove.x) +  " _coordToMove.y: "+ ((int)_coordToMove.y) + " _coordPiece.y: " + ((int)_coordPiece.y));

		if(validMovementBool)
		{
			_fieldPieces[(int)_coordToMove.x, 10-(int)_coordToMove.y] = _fieldPieces[(int)_coordPiece.x, 10-(int)_coordPiece.y];
			_fieldPieces[(int)_coordPiece.x , 10-(int)_coordPiece.y ] = 0;
			
			SelectedPiece.transform.position = new Vector3(_coordToMove.x, _pieceHeight, _coordToMove.y);		// Move the piece
			
			ReturnColorToPiece(); // Change it's color back
			
			SelectedPiece = null; // Unselect the Piece
			if(BallPossesion() == activePlayer)	//si el jugador tiene posesion del balon
			{
				ChangeState (2);//cambia el estado a 2

			}
			else{//si no tiene posesion
					ChangeState (0);//cambia el estado a 0
					ChangeTurn();//cambia de turno
			}
		}
	}



	

	//FUNCION MODIFICADA

	/*
	*
	* @by Mario Acosta, Rafael Zamora
	*/
	bool TestMovement(GameObject _SelectedPiece, Vector2 _coordToMove)
	{
		bool _movementLegalBool = false;
		bool _collisionDetectBool = false;

		Vector2 _coordPiece = new Vector2(_SelectedPiece.transform.localPosition.x, _SelectedPiece.transform.localPosition.z);
		
		int _deltaX = (int)Mathf.Abs(_coordToMove.x - _coordPiece.x);
		int _deltaY = (int)Mathf.Abs(_coordToMove.y - _coordPiece.y); //con valor absoluto
		int activePlayerPawnPostion = 1;

		//Debug.Log ("_coordToMove.y " + (10-_coordToMove.y) + "_coordPiece.y " + _coordPiece.y);


		Debug.Log("Piece:(" + _coordPiece.x + "," + _coordPiece.y + ") Move:(" + _coordToMove.x + "," + ((int)( _coordToMove.y)) + ")");
		//Debug.Log("Piece (" + _coordPiece.x + "," + _coordPiece.y + ") - Move (" + _coordToMove.x + ","((int)(10 + _coordToMove.y)) + ")");
		Debug.Log("Delta (" + _deltaX + "," + _deltaY + ")");

		//Controlamos si la posicion destino se encuentra ocupada por otra ficha (M)
		if (_fieldPieces[(int)_coordToMove.x, (10-(int)_coordToMove.y)] != 0)
		{
			//Si entra aca quiere decir que la casilla ya se encuentra ocupada por otra pieza
		Debug.Log ("La casilla esta ocupada");
		_movementLegalBool = false;
		return _movementLegalBool;
		}
			// Use the name of the _SelectedPiece GameObject to find the piece used

			switch (_SelectedPiece.name)
			{
				case "Ball":
					Debug.Log ("entro a Ball max: " + ((int)Mathf.Max(_deltaX, _deltaY)) + "_deltaX: " + _deltaX + "_deltaY: " + _deltaY);
					// King can only move one
					if( (int)Mathf.Max(_deltaX, _deltaY) <= 4)
					{
						if(_deltaX == _deltaY || ((_deltaX == 0) && (_deltaY != 0)) || ((_deltaX != 0) && (_deltaY == 0))) 
						{
							if(testPossesion((int)_coordToMove.x, (10-(int)_coordToMove.y)) == activePlayer * -1)//si es posesion de otra persona en esa posicion
							{
								_movementLegalBool = false;
								return _movementLegalBool;//retorna false
							}
							else//sino
							{
								_movementLegalBool = true;
							}
						}
					}
			        break;
				case "Player":
					//Debug.Log ("entro a Player max: " + ((int)Mathf.Max(_deltaX, _deltaY)) + " _deltaX: " + _deltaX + " _deltaY: " + _deltaY);
					// King can only move one
					if( (int)Mathf.Max(_deltaX, _deltaY) <= 2)
					{
						if(_deltaX == _deltaY || ((_deltaX == 0) && (_deltaY != 0)) || ((_deltaX != 0) && (_deltaY == 0))) 
						{
							//Debug.Log ("ES VALIDO!");
							_movementLegalBool = true;
						}
						//Debug.Log ("NO ES VALIDO!");
					}

					//Los jugadores no pueden entrar al arco
					if ((int)_coordToMove.x == 0 || (int)_coordToMove.x == 14)
					{
						_movementLegalBool = false;
					}
			        break;
				case "Keeper":
					Debug.Log ("entro a Keeper max: " + ((int)Mathf.Max(_deltaX, _deltaY)) + "_deltaX: " + _deltaX + "_deltaY: " + _deltaY);
					// King can only move one
					if( (int)Mathf.Max(_deltaX, _deltaY) <= 2)
					{
						if(_deltaX == _deltaY || ((_deltaX == 0) && (_deltaY != 0)) || ((_deltaX != 0) && (_deltaY == 0))) 
						{
							_movementLegalBool = true;
						}
					}
					//Los jugadores no pueden entrar al arco
					if ((int)_coordToMove.y == 0 || (int)_coordToMove.y == 14)
					{
						_movementLegalBool = false;
					}
					break;   
			    default:
			        _movementLegalBool = false;
			        break;
			}
		// If the movement is legal, detect collision with piece in the way. Don't do it with the ball since they can pass over pieces TODO: unless is a goalkeeper in his area.
		//Si el movimiento es legal, detectar colision con otra pieza
		if(_movementLegalBool && SelectedPiece.name != "Pelota")
		{
			_collisionDetectBool = TestCollision (_coordPiece, _coordToMove);
		}
			
		return (_movementLegalBool && !_collisionDetectBool);
	}


	
	//FUNCION MODIFICADA
	//TODO: la pelota no puede atravezar ni el arquero ni sus brazos dentro del area
	// Test if a unit is in the path of the tested movement
	bool TestCollision(Vector2 _coordInitial, Vector2 _coordFinal)
	{
		bool CollisionBool = false;
		//los delta se dejan sin valores absolutos
		int _deltaX = (int)(_coordFinal.x - _coordInitial.x);
		int _deltaY = (int)((_coordFinal.y) - _coordInitial.y); 
		int _incX = 0; // Direccion del incremento en X
		int _incY = 0; // Direccion del incremento en X
		int i;
		int j;

		// Cacula el incremento (que puede ser negativo) evita la division por 0
		if(_deltaX != 0)
		{
			_incX = (_deltaX/Mathf.Abs(_deltaX));
		}
		if(_deltaY != 0)
		{
			_incY = (_deltaY/Mathf.Abs(_deltaY));
		}
		
		//Debug.Log ("_coordInit:(" + ((int)_coordInitial.x) + "," + ((int)_coordInitial.y) + ") _coordFinal:(" + ((int)(_coordFinal.x)) + "," + ((int)(_coordFinal.y)) + ")");

		i = (int)_coordInitial.x + _incX;
		j = (int)_coordInitial.y + _incY;
		
		while(new Vector2(i, j) != _coordFinal)
		{
			if(_fieldPieces[i,10-j] != 0)
			{
				CollisionBool = true;
				break;
			}
			i += _incX;
			j += _incY;
		}
		return CollisionBool;
	}



/*
	*	retorna: -1 si player 2 tiene mas jugadores en el cuadrante de la pelota 
				  1 si player 1 tiene mas jugadores en el cuadrante de la pelota 
				  0 si nadie tiene el balon o si hay cantidades iguales de oponentes
				  si es el arquero siempre tiene posesion de la pelota
	 	* funcion posesion de balon
	*	@by Mario Acosta
	*	TODO: modificar para que reciba coordenadas
	*/
	int BallPossesion ()
	{
		int i,j;
		int x,y;
		int x_min=0, x_max=0,y_min=0,y_max=0;
		int p1=0,p2=0;
		Vector2 _currentCoor;
		GameObject _Ball = null;
		_Ball = GameObject.Find("Ball");

		//Guardamos la posicion de la pelota
		_currentCoor = new Vector2(_Ball.transform.position.x, _Ball.transform.position.z);
		x = (int)_currentCoor.x;
		y = 10-(int)_currentCoor.y;
		
		//Establecemos los limites de busqueda min y max para X e Y
			x_min = x-1;
			x_max = x+1;
			y_min = y-1;
			y_max = y+1;

		//En caso que este en el borde izquierdo 
		if (x == 1)
			x_min = x;

		//En caso que este en el borde derecho 
		if (x == 13)
			x_max = x;

		//En caso que este en el borde superior 
		if (y == 0)
			y_min = y;
		//En caso que este en el borde inferior 
		if (y == 10)
			y_max = y;
		//Recorremos las casillas alrededor
		for(i=x_min; i<=x_max;i++)
		{
			for (j = y_min; j <= y_max; j++)
			{
				//Contamos los players alrededor de la pelota
				if (_fieldPieces[i,j] == 1) 
				{
					p1++;
				}
				if (_fieldPieces[i,j]==-1)
				{
					p2++;
				}
			}
		}
		//Retornamos la posesion
		if (p1 == p2)
			return 0;
			if (p1>p2)
			{
				return 1;
			}
			else
			{
				return -1;
			}
	}


	int testPossesion(int _posX, int _posY)
	{
		int i,j;
		int p1=0,p2=0;
		Vector2 _currentCoor;
		GameObject _Ball = null;
		_Ball = GameObject.Find("Ball");
		
		//Declaramos los limites de busqueda min y max para X e Y
		int x_min = _posX-1, x_max = _posX+1, y_min=_posY-1, y_max=_posY+1;

		//En caso que este en el borde izquierdo 
		if (_posX == 1)
			x_min = _posX;

		//En caso que este en el borde derecho 
		if (_posX == 13)
			x_max = _posX;

		//En caso que este en el borde superior 
		if (_posY == 0)
			y_min = _posY;
		//En caso que este en el borde inferior 
		if (_posY == 10)
			y_max = _posY;
		//Recorremos las casillas alrededor
		for(i=x_min; i<=x_max;i++)
		{
			for (j = y_min; j <= y_max; j++)
			{
				//Contamos los players alrededor de la pelota
				if (_fieldPieces[i,j] == 1) 
				{
					p1++;
				}
				if (_fieldPieces[i,j]==-1)
				{
					p2++;
				}
			}
		}
		//Retornamos la posesion
		if (p1 == p2)
			return 0;
			if (p1>p2)
			{
				return 1;
			}
			else
			{
				return -1;
			}
	}


	// Change the state of the game from Piece selection to Piece movement and viceversa
	public void ChangeTurn()
	{
			activePlayer = -activePlayer;
			turnTime = 0;
			gameTurn++;
	}


	// Change the state of the game from Piece selection to Piece movement and viceversa
	public void ChangeState(int _newState)
	{
		gameState = _newState;
	}
}
