using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Tooltip("Generate a map in the editor")]
    public bool generateMap;
    [Tooltip("The X value equals the floor number while the Y value indicates the room number")]
    public Vector2 floor;
    [Tooltip("Higher map difficulty increases the number of dangerous terrains")]
    public int mapDifficulty;
    [Tooltip("Higher enemy difficulty increases the number of enemies/allows for more challenging enemies that cost more")]
    public int enemyDifficulty;
    [HideInInspector] 
    public int xFlip; //used to flip the generated map hortizontally to provide more randomization
    [HideInInspector] 
    public int yFlip; //used to flip the generated map vertically to provide more randomization
    bool firstSpawn = true; //initial spawn room with a shop

    [Header("Map texture references")]
    [Tooltip("The list of maps that could possible spawn. IMPORTANT, the first map will be a shop room. " +
        "If you would like to increase the chance a particular map will spawn, then add more here")]
    public Texture2D[] maps;
    [HideInInspector]
    public Color[] field; //generated map of colours that are read from the texture of one of maps

    [Header("Walls, Boundaries & Ground")]
    [Tooltip("Gameobject used to hold all the walls and boundaries")]
    public GameObject borderHolder;
    [Tooltip("List of different walls/boundaries. Adding more of the same one increases it's spawn chance")]
    public List<GameObject> walls;
    public List<GameObject> boundaries;

    [Tooltip("If true, will add ground sprites to the background. If false will have a blank colour")]
    public bool groundTiles;
    [System.Serializable]
    public class groundTypes
    {
        public string group;//name of group
        public List<GameObject> grounds;//list of ground tiles to spawn
    }
    [Tooltip("List of different ground's and by different themed groupings")]
    public List<groundTypes> groundT;
    [Tooltip("Blank image used if not using ground tiles")]
    public GameObject blankGround;

    [Header("Terrains")]
    [Tooltip("Gameobject used to hold all the terrains (traps)")]
    public GameObject terrainHolder;
    [Tooltip("List of ordinary terrains")]
    public List<GameObject> terrains;
    [Tooltip("List of more challenging terrains. The higher the map difficulty, the more of these terrains will spawn")]
    public List<GameObject> dangerousTerrains;
    int terrainNumber;//used to keep the same type of terrain. make sure to set up similar themed terrain in booth terrain types

    [Header("Enemies")]
    [Tooltip("Gameobject used to hold all the enemies")]
    public GameObject enemyHolder;
    [System.Serializable]
    public class enemyV
    {
        [Tooltip("Floor 1 spawns teir 1 enemies, floor 2 teir 2 e.c.t")]
        public string Tier;
        public List<GameObject> enemy;
    }
    public List<enemyV> enemies;

    [Header("Objects")]
    [Tooltip("Gameobject used to hold all the enemies")]
    public GameObject objectHolder;
    [Tooltip("List of objects")]
    public List<GameObject> objects;

    [Space(5)]
    [Tooltip("Shop gameobject, sprite which triggers shop UI")]
    public GameObject shop;

    //Spawnable Vector Positions. Enemy controller requires access to some spawns
    [HideInInspector] public List<Vector2> groundSpawn;
    [HideInInspector] public List<Vector2> wallSpawn;
    [HideInInspector] public List<Vector2> boundarySpawn;
    [HideInInspector] public List<Vector2> terrainSpawn;
    [HideInInspector] public List<Vector2> enemySpawn;
    [HideInInspector] public List<Vector2> objectSpawn;
    [HideInInspector] public List<Vector2> playerSpawn;

    [Header("Other References")]
    [HideInInspector] public TurnController turnC;
    [Tooltip("Shop controller/UI. Generates the items, screen space e.c.t.")]
    public ShopController shopC;
    [Tooltip("Exit gameobject to spawn when all enemies are killed")]
    public GameObject floorExit;
    [Tooltip("Generates items and their values")]
    public ItemGenerator itemGenerator;

    private void Start()
    {
        turnC = this.GetComponent<TurnController>();
        GenerateMap(0);
    }

    public void DestroyMap()
    {
        //go through each of the sprite holders and delete the children (from last to first)
        int b = borderHolder.transform.childCount;
        for (int i = 0; i < b;)
        {
            Destroy(borderHolder.transform.GetChild(b - 1).gameObject);
            b -= 1;
        }

        int o  = objectHolder.transform.childCount;
        for (int i = 0; i < o;)
        {
            Destroy(objectHolder.transform.GetChild(o - 1).gameObject);
            o -= 1;
        }

        int e = enemyHolder.transform.childCount;
        for (int i = 0; i < e;)
        {
            Destroy(enemyHolder.transform.GetChild(e - 1).gameObject);
            e -= 1;
        }

        int t = terrainHolder.transform.childCount;
        for (int i = 0; i < t;)
        {
            Destroy(terrainHolder.transform.GetChild(t - 1).gameObject);
            t -= 1;
        }

        //create new lists
        wallSpawn = new List<Vector2>();
        boundarySpawn = new List<Vector2>();
        terrainSpawn = new List<Vector2>();
        enemySpawn = new List<Vector2>();
        objectSpawn = new List<Vector2>();
        playerSpawn = new List<Vector2>();
        groundSpawn = new List<Vector2>();

        //generate a new map
        GenerateMap(1);
    }
    public void GenerateMap(int mapNum)
    {
        //randomly select a map from the list
        if (mapNum == 1) 
        {
            mapNum = Random.Range(1, maps.Length);
        }
        //if the player has reached the end of a floor, create the shop room
        if (floor.y == 5)
        {
            mapNum = 0;
            floor.x += 1;
            floor.y = -1;
        }
        //if the player is on the last room of the floor increase the difficulty
        if(floor.y == 4)
        {
            mapDifficulty += 3;
            enemyDifficulty += 3;
        }

        //next room
        floor.y += 1;

        //Show the player the floor/room number
        if (floor.y == 0)
        {
            if (floor.x == 1)
            {
                GameObject.FindGameObjectWithTag("HUD").GetComponent<HUDController>().floor.text = "";
            }
            else
            {
                GameObject.FindGameObjectWithTag("HUD").GetComponent<HUDController>().floor.text = floor.x.ToString() + ":Shop";
            }
        }
        else
        {
            GameObject.FindGameObjectWithTag("HUD").GetComponent<HUDController>().floor.text = floor.x.ToString() + " : " + floor.y.ToString();
        }

        //get the colours of the map
        field = maps[mapNum].GetPixels(0);

        //this is used for inverting the vector2s so that the map flips for more variance        
        xFlip = 1;
        yFlip = 1;
        if (Random.Range(0, 10) <  5)
        {
            xFlip = -1;
        }
        if (Random.Range(0, 10) < 5)
        {
            yFlip = -1;
        }

        //read the colours of the map sprite and add new vector2 positions for the relative spawners
        for (int h = 0; h < maps[mapNum].height; h++)
        {
            for (int w = 0; w < maps[mapNum].width; w++)
            {
                //the array number in field
                int wAndh = h * maps[mapNum].height + w;

                //black border
                if (field[wAndh] == Color.black)
                {
                    wallSpawn.Add(new Vector2(w * xFlip, h * yFlip));
                }

                //magenta boundaries
                if(field[wAndh] == Color.magenta)
                {
                    boundarySpawn.Add(new Vector2(w * xFlip, h * yFlip));
                }

                //white terrain
                else if (field[wAndh] == Color.white)
                {
                    terrainSpawn.Add(new Vector2(w * xFlip, h * yFlip));
                    groundSpawn.Add(new Vector2(w * xFlip, h * yFlip));
                }

                //red enemy
                else if (field[wAndh] == Color.red)
                {
                    enemySpawn.Add(new Vector2(w * xFlip, h * yFlip));
                    groundSpawn.Add(new Vector2(w * xFlip, h * yFlip));
                }

                //blue objects
                else if (field[wAndh] == Color.blue)
                {
                    objectSpawn.Add(new Vector2(w * xFlip, h * yFlip));
                    groundSpawn.Add(new Vector2(w * xFlip, h * yFlip));
                }

                //green player
                else if (field[wAndh] == Color.green)
                {
                    playerSpawn.Add(new Vector2(w * xFlip, h * yFlip));
                    groundSpawn.Add(new Vector2(w * xFlip, h * yFlip));
                }

                //floorexit for shop and spawn
                else if (field[wAndh] == new Color(1,1,0,1))
                {
                    GameObject s = Instantiate(floorExit, new Vector2(w * xFlip, h * yFlip), Quaternion.identity);
                    s.transform.SetParent(terrainHolder.transform);
                }

                else 
                {
                    //if there is nothing, then it is a ground tile
                    if (field[wAndh].a == 0)
                    {
                        groundSpawn.Add(new Vector2(w * xFlip, h * yFlip));
                    }
                }
            }
        }

        //with the map selected and the colours extracted now we create the map
        CreateMap();
    }
    public void CreateMap()
    {
        //go through every player and spawn them at a random spot on the map
        for(int i = 0; i < this.GetComponent<PlayerController>().playerCharacters.Count; i++)
        {
            Vector2 spot = playerSpawn[Random.Range(0, playerSpawn.Count)];
            this.GetComponent<PlayerController>().playerCharacters[i].transform.position = spot;
            playerSpawn.Remove(spot);
        }

        //spawn walls
        foreach (Vector2 gen in wallSpawn)
        {
            GameObject s = Instantiate(walls[Random.Range(0,walls.Count)], gen, Quaternion.identity);
            s.transform.SetParent(borderHolder.transform);
        }

        //spawn boundaries
        foreach (Vector2 gen in boundarySpawn)
        {
            GameObject s = Instantiate(boundaries[Random.Range(0, boundaries.Count)], gen, Quaternion.identity);
            s.transform.SetParent(borderHolder.transform);
            s.gameObject.layer = LayerMask.NameToLayer("Boundary");
        }

        //higher map difficulty, more dangerous terrains
        for (int m = 0; m < mapDifficulty; m++)
        {
            if (terrainSpawn.Count > 0)
            {
                int spot = Random.Range(0, terrainSpawn.Count);
                GameObject s = Instantiate(dangerousTerrains[terrainNumber], terrainSpawn[spot], Quaternion.identity);
                s.transform.SetParent(terrainHolder.transform);
                terrainSpawn.RemoveAt(spot);
            }
        }

        //for every other terrain spawn a normal terrain
        foreach (Vector2 gen in terrainSpawn)
        {
            GameObject s = Instantiate(terrains[terrainNumber], gen, Quaternion.identity);
            s.transform.SetParent(terrainHolder.transform);
        }

        //add a new set of enemies to the turn controller
        turnC.enemies = new List<EnemyController>();
        int enemyTier = Mathf.RoundToInt(floor.x) - 1;
       
        if (enemyTier > enemies.Count)
        {
            //makes sure at least 1 enemy spawns
            enemyTier = enemies.Count - 1;
        }

        //spawn enemies until you have reached the enemy difficulty or you have spawned an enemy on every spawn point possible
        if (enemySpawn.Count > 1 && enemySpawn != null)
        {
            for (int i = 0; i < enemyDifficulty && enemySpawn.Count != 0;)
            {
                int n = Random.Range(0, enemies[enemyTier].enemy.Count);
                int es = Random.Range(0, enemySpawn.Count);
                //if the final enemy will push the value over enemy difficulty then use an enemy from the previous teir
                if (i + enemies[enemyTier].enemy[n].GetComponent<EnemyController>().enemyValue > enemyDifficulty && enemyTier != 0)
                {
                    enemyTier -= 1;
                    n = Random.Range(0, enemies[enemyTier].enemy.Count);
                    es = Random.Range(0, enemySpawn.Count);
                }
                GameObject s = Instantiate(enemies[enemyTier].enemy[n], enemySpawn[es], Quaternion.identity);
                s.transform.SetParent(enemyHolder.transform);
                s.gameObject.name = enemies[enemyTier].enemy[n].name;
                enemySpawn.Remove(enemySpawn[es]);
                i += enemies[enemyTier].enemy[n].GetComponent<EnemyController>().enemyValue;
                turnC.enemies.Add(s.GetComponent<EnemyController>()); 
            }
        }

        //first room spawn a high value object
        if (firstSpawn)
        {
            int spawn1 = Random.Range(0, objectSpawn.Count);
            GameObject s = Instantiate(shop, objectSpawn[spawn1], Quaternion.identity);
            s.transform.SetParent(enemyHolder.transform);
            shopC.shopSpot = s;
            shopC.GenerateShop();
            objectSpawn.RemoveAt(spawn1);

            //spawn a high value drop
            spawn1 = Random.Range(0, objectSpawn.Count);
            s = Instantiate(objects[Random.Range(0, objects.Count)], objectSpawn[spawn1], Quaternion.identity);
            s.transform.SetParent(enemyHolder.transform);
            objectSpawn.RemoveAt(spawn1);

            firstSpawn = false;
        }

        //normal room spawning
        else if (floor.y > 0)
        {
            for (int i = 0; i < 2; i++)
            {
                if (0 < objectSpawn.Count)
                {
                    int spawn = Random.Range(0, objectSpawn.Count);
                    itemGenerator.GenerateItem(objectSpawn[spawn], new int[0], Random.Range(0, 4), false);
                    objectSpawn.RemoveAt(spawn);
                }
                else
                {
                    i = Mathf.RoundToInt(floor.x);
                }
            }
        }

        //shop room
        else
        {
            int spawn1 = Random.Range(0, objectSpawn.Count);
            GameObject s = Instantiate(shop, objectSpawn[spawn1], Quaternion.identity);
            s.transform.SetParent(enemyHolder.transform);
            shopC.shopSpot = s;
            shopC.GenerateShop();

            //the further you go the more high value drops you get
            for (int i = 0; i < floor.x; i++)
            {
                if (0 < objectSpawn.Count)
                {
                    spawn1 = Random.Range(0, objectSpawn.Count);
                    s = Instantiate(objects[Random.Range(0,objects.Count)], objectSpawn[spawn1], Quaternion.identity);
                    s.transform.SetParent(enemyHolder.transform);
                    objectSpawn.RemoveAt(spawn1);
                }
                else
                {
                    i = Mathf.RoundToInt(floor.x);
                }
            }
        }

        //randomize which ground tiles you get
        int g = Random.Range(0, 2);
        //ground tile spawner
        foreach (Vector2 gen in groundSpawn)
        {
            if (groundTiles)
            {
                GameObject s = Instantiate(groundT[g].grounds[Random.Range(0, groundT[g].grounds.Count)], gen, Quaternion.identity);
                s.transform.SetParent(borderHolder.transform);
            }
            else
            {
                GameObject s = Instantiate(blankGround, gen, Quaternion.identity);
                s.transform.SetParent(borderHolder.transform);
            }
        }

        //reset the player's action points (plays cleaner)
        PlayerController playerC = this.GetComponent<PlayerController>();
        playerC.actionPoints = playerC.actionPointsStart;
        //give them half their mana back
        playerC.manaPoints += Mathf.CeilToInt(playerC.manaPointsCap / 2);
        if(playerC.manaPoints > playerC.manaPointsCap)
        {
            playerC.manaPoints = playerC.manaPointsCap;
        }
        playerC.PlayerTextUpdpate();
    }

    private void Update()
    {
        //used in editor to generate a new map
        if(generateMap)
        {
            generateMap = false;
            DestroyMap();
        }
    }
}
