using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace CaveTreasure
{
    class Program
    {
        // *********** Settings ****************
        // **** Map ****
        public static char wallChar = '█';
        public static ConsoleColor wallColor = ConsoleColor.DarkGreen;
        public static int spawnAliveProbability = 25;
        // **** Player ****
        public static ConsoleColor playerColor = ConsoleColor.Cyan;
        public static char playerChar = '■';
        public static char playerTrail = ' ';
        public static int[] playerPosition;
        // **** Treasure ****
        public static ConsoleColor treasureColor = ConsoleColor.Yellow;
        public static char treasureChar = '$';
        // **** Enemy ****
        public static ConsoleColor enemyColor = ConsoleColor.Red;
        public static char enemyChar = '■';
        // **** Exit ****
        public static ConsoleColor exitColor = ConsoleColor.Magenta;
        public static char exitChar = '%';
        public static int[] exitPosition;
        // **** Beta Tools ****
        public static bool enableBetaTools = true;
               
        public static Random r1 = new Random();
        public static List<int[]> wallPositions = new List<int[]>();
        public static List<int[]> treasurePositions = new List<int[]>();
        public static List<int[]> enemyPositions = new List<int[]>();

        

        static void Main(string[] args)
        {
            
            ThreadStart npcTasks = new ThreadStart(EnemyMoveSeq);
            Thread t1 = new Thread(npcTasks);

            // **** Game Setup ****
            int levelDiff = 15;
            int score = 0;
            int health = 5;
            int level = 1;
            bool playerAlive = true;

            // **** Initialization ****
            TitleScreen();
            Console.ReadLine();
            Console.Clear();

            // **** Game Loop ****
            while (playerAlive)
            {
                bool levelComplete = false;
                GenerateMap(levelDiff + (score / 10000), r1.Next(10,50) - level, r1.Next(3,10) + level);
                DisplayGameData(score, level, health);
                if (!t1.IsAlive)
                {
                    t1.Start();                    
                }

                else
                {
                    t1.Resume();
                }

                Thread.Sleep(100);

                // **** Level Loop ****
                while (!levelComplete)
                {                   
                    Console.CursorVisible = false;
                    
                    switch (Console.ReadKey(true).Key)
                    {
                        // Test Spawn Controls
                        case ConsoleKey.P:
                            ClearSquare(playerPosition);
                            SpawnPlayer();
                            break;

                        case ConsoleKey.T:
                            SpawnTreasure(5);
                            break;

                        case ConsoleKey.E:
                            SpawnEnemy(3);
                            break;

                        case ConsoleKey.V:
                            ClearSquare(exitPosition);
                            SpawnExit();
                            break;

                        case ConsoleKey.M:
                            MoveEnemies();
                            break;

                        case ConsoleKey.Q:
                            SetPlayerPosition(exitPosition);
                            break;

                        case ConsoleKey.K:
                            health = 0;
                            break;

                        // Player Controls
                        case ConsoleKey.UpArrow:
                            MovePlayerUp();
                            break;

                        case ConsoleKey.DownArrow:
                            MovePlayerDown();
                            break;

                        case ConsoleKey.LeftArrow:
                            MovePlayerLeft();
                            break;

                        case ConsoleKey.RightArrow:
                            MovePlayerRight();
                            break;

                        default:
                           // MoveEnemies();
                            break;
                    }

                    // Check if player is at exit
                    if (playerPosition[0] == exitPosition[0] && playerPosition[1] == exitPosition[1])
                    {
                        Console.Clear();
                        ClearMapData();
                        levelDiff += 2;
                        score += 1000;
                        level++;
                        levelComplete = true;
                        t1.Suspend();
                        DisplayGameData(score, level, health);
                    }

                    // Respond to non empty spaces
                    if (!IsEmptySpace(playerPosition))
                    {
                        if (IsEnemy(playerPosition))
                        {
                            health--;
                            score += r1.Next(1,250);
                            levelDiff -= 2;
                            KillEnemy(playerPosition);
                            DisplayGameData(score, level, health);
                        }

                        else if (IsTreasure(playerPosition))
                        {
                            score += r1.Next(1,100);
                            RemoveTreasure(playerPosition);
                            DisplayGameData(score, level, health);
                        }
                    }

                    // Is player alive
                    if (health == 0)
                    {
                        Console.Clear();
                        playerAlive = false;
                        t1.Abort();
                        break;
                    }
                   
                }
                             
            }

            GameOver();

            Console.ReadLine();                                          

        }

        // ****** Map Gen ******
        private static bool[,] InitializeMap(bool[,] map, int spawnAliveProb)
        {
            for (int x = 0; x < map.GetLength(0); x++)
            {
                for (int y = 0; y < map.GetLength(1); y++)
                {
                    if (r1.Next(1,100) < spawnAliveProb)
                    {
                        map[x, y] = true;
                    }
                }
            }

            return map;
        }

        private static int CountAliveNeighours(bool[,] oldmap, int x, int y)
        {
            int count = 0;

            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    int neighbour_x = x + i;
                    int neighbour_y = y + j;

                    if (i == 0 && j == 0)
                    {

                    }

                    else if (neighbour_x < 0 || neighbour_y < 0 || neighbour_x >= oldmap.GetLength(0) || neighbour_y >= oldmap.GetLength(1))
                    {
                        count += 1;
                    }

                    else if (oldmap[neighbour_x, neighbour_y])
                    {
                        count += 1;
                    }
                }
            }

            return count;
        }

        private static bool[,] DoSimulationStep(bool[,] oldmap, int deathLimit, int birthLimit)
        {
            bool[,] newMap = new bool[oldmap.GetLength(0), oldmap.GetLength(1)];

            for (int x = 0; x < oldmap.GetLength(0); x++)
            {
                for (int y = 0; y < oldmap.GetLength(1); y++)
                {
                    int nbs = CountAliveNeighours(oldmap, x, y);

                    if (oldmap[x,y])
                    {
                        if (nbs < deathLimit)
                        {
                            newMap[x, y] = false;
                        }

                        else
                        {
                            newMap[x, y] = true;
                        }
                    }

                    else
                    {
                        if (nbs > birthLimit)
                        {
                            newMap[x, y] = true;
                        }

                        else
                        {
                            newMap[x, y] = false;
                        }
                    }
                }
            }

            return newMap;
        }

        private static void PrintWalls(bool[,] map)
        {
            for (int x = 0; x < map.GetLength(0); x++)
            {
                for (int y = 0; y < map.GetLength(1); y++)
                {
                    if (map[x,y] == true)
                    {
                        int[] pos = { x, y };
                        wallPositions.Add(pos);

                        Console.ForegroundColor = wallColor;
                        Console.SetCursorPosition(x, y);
                        Console.Write(wallChar);
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                }
            }
        }
        // ****** Spawning ******
        private static void DisplayGameData(int score, int level, int health)
        {
            Console.SetCursorPosition(0, 0);
            Console.BackgroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.DarkBlue;

            for (int i = 0; i < Console.WindowWidth; i++)
            {
                Console.Write(" ");
            }

            Console.SetCursorPosition(0, 0);

            Console.Write(string.Format("Level:{0}\t\t", level));
            Console.Write(string.Format("Score:{0}\t\t", score));
            Console.Write("Health:");

            Console.ForegroundColor = ConsoleColor.Red;

            for (int i = 0; i < health; i++)
            {
                Console.Write("■");
            }

            Console.BackgroundColor = ConsoleColor.Black;
        }

        private static void SpawnWalls(int steps)
        {
            bool[,] cellMap = new bool[Console.BufferWidth, Console.WindowHeight];
            bool[,] initialMap = InitializeMap(cellMap, 38);

            bool[,] map = DoSimulationStep(initialMap, 3, 4);
            bool[,] nextmap = new bool[map.GetLength(0), map.GetLength(1)];

            for (int i = 0; i < steps; i++)
            {
                nextmap = DoSimulationStep(map, 3, 4);

                map = nextmap;
            }

            PrintWalls(nextmap);
        }

        private static void SpawnPlayer()
        {
            bool vaildSpawn = false;

            while (!vaildSpawn)
            {
                int[] randomPos = { r1.Next(0, Console.WindowWidth), r1.Next(0, Console.WindowHeight) };

                if (!IsWall(randomPos) && IsInsidePlayArea(randomPos))
                {
                    Console.ForegroundColor = playerColor;
                    Console.SetCursorPosition(randomPos[0], randomPos[1]);
                    Console.Write(playerChar);
                    playerPosition = randomPos;
                    vaildSpawn = true;
                }
            }

            Console.ForegroundColor = ConsoleColor.White;

        }

        private static void SpawnTreasure(int quantity)
        {
            for (int i = 0; i < quantity; i++)
            {
                bool vaildSpawn = false;

                while (!vaildSpawn)
                {
                    int[] randomPos = { r1.Next(0, Console.WindowWidth), r1.Next(0, Console.WindowHeight) };

                    if (IsEmptySpace(randomPos))
                    {
                        Console.ForegroundColor = treasureColor;
                        Console.SetCursorPosition(randomPos[0], randomPos[1]);
                        Console.Write(treasureChar);
                        treasurePositions.Add(randomPos);
                        vaildSpawn = true;
                    }
                }
            }
                       
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void SpawnEnemy(int quantity)
        {
            for (int i = 0; i < quantity; i++)
            {
                bool vaildSpawn = false;

                while (!vaildSpawn)
                {
                    int[] randomPos = { r1.Next(0, Console.WindowWidth), r1.Next(0, Console.WindowHeight) };

                    if (IsEmptySpace(randomPos))
                    {
                        Console.ForegroundColor = enemyColor;
                        Console.SetCursorPosition(randomPos[0], randomPos[1]);
                        Console.Write(enemyChar);
                        enemyPositions.Add(randomPos);
                        vaildSpawn = true;
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void SpawnExit()
        {
             bool vaildSpawn = false;
             int spawnWall = r1.Next(0, 4);

                while (!vaildSpawn)
                {
                int[] randomPos = new int[2];

                    switch (spawnWall)
                    {
                        case 0:
                        randomPos[0] = 0;
                        randomPos[1] = r1.Next(1, Console.WindowHeight);
                            break;

                        case 1:
                        randomPos[0] = r1.Next(0, Console.WindowWidth);
                        randomPos[1] = 1;
                        break;

                        case 2:
                        randomPos[0] = Console.WindowWidth - 1;
                        randomPos[1] = r1.Next(1, Console.WindowHeight);
                        break;

                        case 3:
                        randomPos[0] = r1.Next(0, Console.WindowWidth);
                        randomPos[1] = Console.WindowHeight - 1;
                        break;

                    }

                if (IsEmptySpace(randomPos))
                {
                    Console.ForegroundColor = exitColor;
                    Console.SetCursorPosition(randomPos[0], randomPos[1]);
                    Console.Write(exitChar);
                    exitPosition = randomPos;
                    vaildSpawn = true;
                }


            }
        }

        private static void GenerateMap(int wallSpawnPercentage, int quntTreasure, int quantEnemy)
        {
            int environment = r1.Next(0, 4);

            switch (environment)
            {
                case 0:
                    wallColor = ConsoleColor.DarkGreen;
                    break;

                case 1:
                    wallColor = ConsoleColor.DarkRed;
                    break;

                case 2:
                    wallColor = ConsoleColor.DarkCyan;
                    break;

                case 3:
                    wallColor = ConsoleColor.DarkMagenta;
                    break;
            }


            spawnAliveProbability = wallSpawnPercentage;

            SpawnWalls(3);
            SpawnPlayer();
            SpawnTreasure(quntTreasure);
            SpawnEnemy(quantEnemy);
            SpawnExit();
        }

        private static void ClearMapData()
        {
            wallPositions.Clear();
            treasurePositions.Clear();
            enemyPositions.Clear();
        }

        private static void RemoveTreasure(int[] pos)
        {
            int posX = pos[0];
            int posY = pos[1];
            int index = 0;

            foreach (int[] TresurePos in treasurePositions)
            {
                if (TresurePos[0] == posX && TresurePos[1] == posY)
                {
                    treasurePositions.RemoveAt(index);
                    break;
                }

                index++;
            }
        }

        private static void KillEnemy(int[] pos)
        {
            int posX = pos[0];
            int posY = pos[1];
            int index = 0;

            foreach (int[] EnemyPos in enemyPositions)
            {
                if (EnemyPos[0] == posX && EnemyPos[1] == posY)
                {
                    enemyPositions.RemoveAt(index);
                    break;
                }

                index++;
            }
        }

        // ****** Checks ******
        private static bool IsWall(int[] pos)
        {
            bool wall = false;
            int posX = pos[0];
            int posY = pos[1];

            foreach (int[] WallPos in wallPositions)
            {
                if (WallPos[0] == posX && WallPos[1] == posY)
                {
                    wall = true;
                }
            }

            return wall;
        }

        private static bool IsTreasure(int[] pos)
        {
            bool treasure = false;
            int posX = pos[0];
            int posY = pos[1];

            foreach (int[] TresurePos in treasurePositions)
            {
                if (TresurePos[0] == posX && TresurePos[1] == posY)
                {
                    treasure = true;
                }
            }

            return treasure;
        }

        private static bool IsEnemy(int[] pos)
        {
            bool enemy = false;
            int posX = pos[0];
            int posY = pos[1];

            foreach (int[] EnemyPos in enemyPositions)
            {
                if (EnemyPos[0] == posX && EnemyPos[1] == posY)
                {
                    enemy = true;
                }
            }

            return enemy;
        }

        private static bool IsPlayer(int[] pos)
        {
            bool player = false;

            if (pos[0] == playerPosition [0] && pos[1] == playerPosition[1])
            {
                player = true;
            }

            return player;
        }

        private static bool IsEmptySpace(int[] pos)
        {
            bool empty = true;

            if (IsWall(pos) || IsTreasure(pos) || IsEnemy(pos)|| IsPlayer(pos) || !IsInsidePlayArea(pos))
            {
                empty = false;
            }

            return empty;
        }

        private static bool IsInsidePlayArea(int[] pos)
        {
            bool InPlayArea = false;

            if (pos[0] <= Console.WindowWidth -1 && pos[0] >= 0 && pos[1] >= 1 && pos[1] < Console.WindowHeight)
            {
                InPlayArea = true;
            }

            return InPlayArea;
        }

        private static bool IsExit(int[] pos)
        {
            bool exit = false;

            if (pos[0] == exitPosition[0] && pos[1] == exitPosition[1])
            {
                exit = false;
            }

            return exit;
        }

        // ****** Movement ******

        private static void ClearSquare(int[] pos)
        {
            Console.SetCursorPosition(pos[0], pos[1]);
            Console.Write(" ");
        }

        private static void ClearSquare(int[] pos, char trailChar)
        {
            Console.SetCursorPosition(pos[0], pos[1]);
            Console.Write(trailChar);
        }

        private static void MovePlayerUp()
        {
            int[] newPostion = { playerPosition[0], playerPosition[1] - 1 };

            if (IsInsidePlayArea(newPostion) && !IsWall(newPostion))
            {
                Console.ForegroundColor = playerColor;
                ClearSquare(playerPosition, playerTrail);
                Console.SetCursorPosition(newPostion[0], newPostion[1]);
                Console.Write(playerChar);
                playerPosition = newPostion;
            }
        }

        private static void MovePlayerDown()
        {
            int[] newPostion = { playerPosition[0], playerPosition[1] + 1 };

            if (IsInsidePlayArea(newPostion) && !IsWall(newPostion))
            {
                Console.ForegroundColor = playerColor;
                ClearSquare(playerPosition, playerTrail);
                Console.SetCursorPosition(newPostion[0], newPostion[1]);
                Console.Write(playerChar);
                playerPosition = newPostion;
            }
        }

        private static void MovePlayerLeft()
        {
            int[] newPostion = { playerPosition[0] - 1, playerPosition[1]};

            if (IsInsidePlayArea(newPostion) && !IsWall(newPostion))
            {
                Console.ForegroundColor = playerColor;
                ClearSquare(playerPosition, playerTrail);
                Console.SetCursorPosition(newPostion[0], newPostion[1]);
                Console.Write(playerChar);
                playerPosition = newPostion;
            }
        }

        private static void MovePlayerRight()
        {
            int[] newPostion = {playerPosition[0] + 1, playerPosition[1]};

            if (IsInsidePlayArea(newPostion) && !IsWall(newPostion))
            {
                Console.ForegroundColor = playerColor;
                ClearSquare(playerPosition, playerTrail);
                Console.SetCursorPosition(newPostion[0], newPostion[1]);
                Console.Write(playerChar);
                playerPosition = newPostion;
            }
        }

        private static void SetPlayerPosition(int[] pos)
        {
            ClearSquare(playerPosition);
            Console.SetCursorPosition(pos[0], pos[1]);
            Console.Write(playerChar);
            playerPosition = pos;
        }

        private static void MoveEnemies()
        {
            List<int[]> newPosList = new List<int[]>();

            foreach (int[] enemy in enemyPositions)
            {
                bool validMove = false;
                int currentPosX = enemy[0];
                int currentPosY = enemy[1];
                int[] newPos = new int[2];

                ClearSquare(enemy, ' ');

                              
                while (!validMove)
                {
                    int moveDir = r1.Next(0,4);

                    moveDir = DirTowardsPlayer(playerPosition);

                    switch (moveDir)
                    {
                        case 0:
                            currentPosY--;
                            break;

                        case 1:
                            currentPosY++;
                            break;

                        case 2:
                            currentPosX--;
                            break;

                        case 3:
                            currentPosX++;
                            break;

                        default:
                            //moveDir = DirTowardsPlayer(playerPosition);
                            break;
                    }

                    newPos[0] = currentPosX;
                    newPos[1] = currentPosY;

                    if (IsInsidePlayArea(newPos) && !IsExit(newPos) && !IsWall(newPos))
                    {
                        validMove = true;
                        newPosList.Add(newPos);
                    }
                   
                }

                Console.SetCursorPosition(newPos[0], newPos[1]);
                Console.ForegroundColor = enemyColor;
                Console.Write(enemyChar);                
            }

            enemyPositions.Clear();

            foreach (int[] newPos in newPosList)
            {
                enemyPositions.Add(newPos);
            }

        }

        private static int DirTowardsPlayer(int[] playerPos)
        {
            int axis = r1.Next(0, 2);
            int dir;
            int distanceToPlayerXStart = Math.Abs(playerPos[0] - playerPosition[0]);
            int distanceToPlayerYStart = Math.Abs(playerPos[1] - playerPosition[1]);
            int x;
            int y;

            if (axis == 0)
            {
                int moveLeftDist = Math.Abs((playerPos[0] - 1) - playerPosition[0]);
                int moveRightDist = Math.Abs((playerPos[0] + 1) - playerPosition[0]);

                if (moveLeftDist <= moveRightDist)
                {
                    dir = 2;
                }

                else
                {
                    dir = 3;
                }
            }

            else
            {
                int moveUpDist = Math.Abs((playerPos[1] - 1) - playerPosition[1]);
                int moveDownDist = Math.Abs((playerPos[1] + 1) - playerPosition[1]);

                if (moveUpDist <= moveDownDist)
                {
                    dir = 0;
                }

                else
                {
                    dir = 1;
                }
            } 



            return dir;
        }

        private static void EnemyMoveSeq()
        {
            while (true)
            {
                Thread.Sleep(50);
                MoveEnemies();
                System.Threading.Thread.Sleep(300);                
            }
        }

        // ****** Screens ******

        private static void TitleScreen()
        {
            //ASCII Title
            Console.WriteLine("     ");
            Console.WriteLine("                            ___                  _____                                   ");
            Console.WriteLine("                           / __\\__ ___   _____  /__   \\_ __ ___  __ _ ___ _   _ _ __ ___ ");
            Console.WriteLine("                          / /  / _` \\ \\ / / _ \\   / /\\| '__/ _ \\/ _` / __| | | | '__/ _ \\");
            Console.WriteLine("                         / /__| (_| |\\ V |  __/  / /  | | |  __| (_| \\__ | |_| | | |  __/");
            Console.WriteLine("                         \\____/\\__,_| \\_/ \\___|  \\/   |_|  \\___|\\__,_|___/\\__,_|_|  \\___|");
            Console.WriteLine("");
            Console.WriteLine("");

            //Instructions
            Console.WriteLine("The mad king has become paranoid about his riches, so he has taken to hinding his wealth all over the kindgdom is deep " +
                "\ncaves. The concept of the game is pretty simple. The aim it to get as much of The Mad King's gold as you can without \ngetting stuck" +
                " or killed by The Mad King's warriors. The deeper you get the harder it gets. Good luck!");
            Console.WriteLine("");

            // Controls
            Console.WriteLine("************");
            Console.WriteLine("* Controls *");
            Console.WriteLine("************");
            Console.WriteLine("");
            ControlDescription("UP ARROW", "Moves the player up");
            ControlDescription("DOWN ARROW", "Moves the player down");
            ControlDescription("LEFT ARROW", "Moves the player left");
            ControlDescription("RIGHT ARROW", "Moves the player right");

            if (enableBetaTools)
            {
                ControlDescription("V", "Respawns exit if you're stuck");
                ControlDescription("P", "Respawns player if you're stuck");
                ControlDescription("T", "Spawns 5 extra tresure if you're stuck");
                ControlDescription("E", "Spawns 3 extra enemies...because why not?");
                ControlDescription("K", "Suicide button.... it is useful for testing");
                ControlDescription("Q", "Teleport to level exit");
            }

            // Key
            Console.SetCursorPosition(60, 12);
            Console.Write("*******");
            Console.SetCursorPosition(60, 13);
            Console.Write("* Key *");
            Console.SetCursorPosition(60, 14);
            Console.Write("*******");

            Console.SetCursorPosition(60, 16);
            Console.ForegroundColor = playerColor;
            Console.Write(playerChar);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" Player position");

            Console.SetCursorPosition(60, 17);
            Console.ForegroundColor = treasureColor;
            Console.Write(treasureChar);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" Treasure (Worth between $1 and $100");

            Console.SetCursorPosition(60, 18);
            Console.ForegroundColor = wallColor;
            Console.Write(wallChar);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" Wall (Varies with each cave)");

            Console.SetCursorPosition(60, 19);
            Console.ForegroundColor = enemyColor;
            Console.Write(enemyChar);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" Enemy (Loot of between $1 and $200 but cost 1 HP)");

            Console.SetCursorPosition(60, 20);
            Console.ForegroundColor = exitColor;
            Console.Write(exitChar);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" Exit (Head for this to go to next level $1000)");

            Console.CursorVisible = false;

        }

        private static void ControlDescription(string button, string description)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(button);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(": ");
            Console.Write(description + "\n");
        }

        private static void GameOver()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.SetCursorPosition(0, 4);

            Console.WriteLine("      ");
            Console.WriteLine("                                       ___                         ___                ");
            Console.WriteLine("                                      / _ \\__ _ _ __ ___   ___    /___|_   _____ _ __ ");
            Console.WriteLine("                                     / /_\\/ _` | '_ ` _ \\ / _ \\  //  /\\ \\ / / _ | '__|");
            Console.WriteLine("                                    / /_\\| (_| | | | | | |  __/ / \\_// \\ V |  __| |   ");
            Console.WriteLine("                                    \\____/\\__,_|_| |_| |_|\\___| \\___/   \\_/ \\___|_|   ");

            Console.SetCursorPosition(50, 11);
            Console.WriteLine("The Mad King got you!!");
        }



    }
}
