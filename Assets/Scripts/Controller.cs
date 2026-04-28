using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    //GameObjects
    public GameObject board;
    public GameObject[] cops = new GameObject[2];
    public GameObject robber;
    public Text rounds;
    public Text finalMessage;
    public Button playAgainButton;

    //Otras variables
    Tile[] tiles = new Tile[Constants.NumTiles];
    private int roundCount = 0;
    private int state;
    private int clickedTile = -1;
    private int clickedCop = 0;
                    
    void Start()
    {        
        InitTiles();
        InitAdjacencyLists();
        state = Constants.Init;
    }
        
    //Rellenamos el array de casillas y posicionamos las fichas
    void InitTiles()
    {
        for (int fil = 0; fil < Constants.TilesPerRow; fil++)
        {
            GameObject rowchild = board.transform.GetChild(fil).gameObject;            

            for (int col = 0; col < Constants.TilesPerRow; col++)
            {
                GameObject tilechild = rowchild.transform.GetChild(col).gameObject;                
                tiles[fil * Constants.TilesPerRow + col] = tilechild.GetComponent<Tile>();                         
            }
        }
                
        cops[0].GetComponent<CopMove>().currentTile=Constants.InitialCop0;
        cops[1].GetComponent<CopMove>().currentTile=Constants.InitialCop1;
        robber.GetComponent<RobberMove>().currentTile=Constants.InitialRobber;           
    }

    public void InitAdjacencyLists()
    {
        // Matriz de adyacencia
        int[,] matriu = new int[Constants.NumTiles, Constants.NumTiles];

        // Inicializamos toda la matriz a 0
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            for (int j = 0; j < Constants.NumTiles; j++)
            {
                matriu[i, j] = 0;
            }
        }

        // Recorremos todas las casillas del tablero
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            int fila = i / Constants.TilesPerRow;
            int columna = i % Constants.TilesPerRow;

            // Limpiamos la lista de adyacencia por si acaso
            tiles[i].adjacency.Clear();

            // Abajo
            if (fila > 0)
            {
                int abajo = i - Constants.TilesPerRow;
                matriu[i, abajo] = 1;
                tiles[i].adjacency.Add(abajo);
            }

            // Arriba
            if (fila < Constants.TilesPerRow - 1)
            {
                int arriba = i + Constants.TilesPerRow;
                matriu[i, arriba] = 1;
                tiles[i].adjacency.Add(arriba);
            }

            // Izquierda
            if (columna > 0)
            {
                int izquierda = i - 1;
                matriu[i, izquierda] = 1;
                tiles[i].adjacency.Add(izquierda);
            }

            // Derecha
            if (columna < Constants.TilesPerRow - 1)
            {
                int derecha = i + 1;
                matriu[i, derecha] = 1;
                tiles[i].adjacency.Add(derecha);
            }
        }
    }

    //Reseteamos cada casilla: color, padre, distancia y visitada
    public void ResetTiles()
    {        
        foreach (Tile tile in tiles)
        {
            tile.Reset();
        }
    }

    public void ClickOnCop(int cop_id)
    {
        switch (state)
        {
            case Constants.Init:
            case Constants.CopSelected:                
                clickedCop = cop_id;
                clickedTile = cops[cop_id].GetComponent<CopMove>().currentTile;
                tiles[clickedTile].current = true;

                ResetTiles();
                FindSelectableTiles(true);

                state = Constants.CopSelected;                
                break;            
        }
    }

    public void ClickOnTile(int t)
    {                     
        clickedTile = t;

        switch (state)
        {            
            case Constants.CopSelected:
                //Si es una casilla roja, nos movemos
                if (tiles[clickedTile].selectable)
                {                  
                    cops[clickedCop].GetComponent<CopMove>().MoveToTile(tiles[clickedTile]);
                    cops[clickedCop].GetComponent<CopMove>().currentTile=tiles[clickedTile].numTile;
                    tiles[clickedTile].current = true;   
                    
                    state = Constants.TileSelected;
                }                
                break;
            case Constants.TileSelected:
                state = Constants.Init;
                break;
            case Constants.RobberTurn:
                state = Constants.Init;
                break;
        }
    }

    public void FinishTurn()
    {
        switch (state)
        {            
            case Constants.TileSelected:
                ResetTiles();

                state = Constants.RobberTurn;
                RobberTurn();
                break;
            case Constants.RobberTurn:                
                ResetTiles();
                IncreaseRoundCount();
                if (roundCount <= Constants.MaxRounds)
                    state = Constants.Init;
                else
                    EndGame(false);
                break;
        }

    }

    public void RobberTurn()
    {
        clickedTile = robber.GetComponent<RobberMove>().currentTile;
        tiles[clickedTile].current = true;

        FindSelectableTiles(false);

        // Guardamos todas las casillas seleccionables en una lista
        List<Tile> selectableTiles = new List<Tile>();

        for (int i = 0; i < Constants.NumTiles; i++)
        {
            if (tiles[i].selectable)
            {
                selectableTiles.Add(tiles[i]);
            }
        }

        // Si hay casillas disponibles, elegimos una aleatoria
        if (selectableTiles.Count > 0)
        {
            int randomIndex = Random.Range(0, selectableTiles.Count);
            Tile selectedTile = selectableTiles[randomIndex];

            robber.GetComponent<RobberMove>().MoveToTile(selectedTile);
            robber.GetComponent<RobberMove>().currentTile = selectedTile.numTile;
        }
        else
        {
            // Caso raro: si no hubiera casillas disponibles, se queda donde está
            robber.GetComponent<RobberMove>().MoveToTile(tiles[robber.GetComponent<RobberMove>().currentTile]);
        }
    }

    public void EndGame(bool end)
    {
        if(end)
            finalMessage.text = "You Win!";
        else
            finalMessage.text = "You Lose!";
        playAgainButton.interactable = true;
        state = Constants.End;
    }

    public void PlayAgain()
    {
        cops[0].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop0]);
        cops[1].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop1]);
        robber.GetComponent<RobberMove>().Restart(tiles[Constants.InitialRobber]);
                
        ResetTiles();

        playAgainButton.interactable = false;
        finalMessage.text = "";
        roundCount = 0;
        rounds.text = "Rounds: ";

        state = Constants.Restarting;
    }

    public void InitGame()
    {
        state = Constants.Init;
         
    }

    public void IncreaseRoundCount()
    {
        roundCount++;
        rounds.text = "Rounds: " + roundCount;
    }

    public void FindSelectableTiles(bool cop)
    {
        int indexcurrentTile;

        if (cop == true)
            indexcurrentTile = cops[clickedCop].GetComponent<CopMove>().currentTile;
        else
            indexcurrentTile = robber.GetComponent<RobberMove>().currentTile;

        // La ponemos rosa porque acabamos de hacer un reset
        tiles[indexcurrentTile].current = true;

        // Cola para el BFS
        Queue<Tile> nodes = new Queue<Tile>();

        // Inicializamos el nodo inicial
        Tile startTile = tiles[indexcurrentTile];
        startTile.visited = true;
        startTile.distance = 0;
        startTile.parent = null;

        nodes.Enqueue(startTile);

        // Si estamos moviendo un policía, necesitamos saber dónde está el otro policía
        int otherCopTile = -1;

        if (cop == true)
        {
            int otherCop = 1 - clickedCop;
            otherCopTile = cops[otherCop].GetComponent<CopMove>().currentTile;
        }

        // BFS limitado a distancia 2
        while (nodes.Count > 0)
        {
            Tile current = nodes.Dequeue();

            // Solo expandimos si estamos a menos de 2 movimientos
            if (current.distance < Constants.Distance)
            {
                foreach (int adjacentIndex in current.adjacency)
                {
                    Tile adjacentTile = tiles[adjacentIndex];

                    // Si es un policía, no puede atravesar la casilla del otro policía
                    if (cop == true && adjacentIndex == otherCopTile)
                    {
                        continue;
                    }

                    // Si no lo hemos visitado todavía
                    if (!adjacentTile.visited)
                    {
                        adjacentTile.visited = true;
                        adjacentTile.distance = current.distance + 1;
                        adjacentTile.parent = current;

                        nodes.Enqueue(adjacentTile);

                        // La casilla inicial no debe ser seleccionable
                        if (adjacentIndex != indexcurrentTile)
                        {
                            adjacentTile.selectable = true;
                        }
                    }
                }
            }
        }
    }









}
