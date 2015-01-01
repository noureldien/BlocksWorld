using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BlocksWorldBuzzle
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Constants

        /// <summary>
        /// empty/blank tile.
        /// </summary>
        private const char TileE = '-';
        /// <summary>
        /// letter-a tile.
        /// </summary>
        private const char TileA = 'a';
        /// <summary>
        /// letter-b tile.
        /// </summary>
        private const char TileB = 'b';
        /// <summary>
        /// letter-c tile.
        /// </summary>
        private const char TileC = 'c';
        /// <summary>
        /// agent tile.
        /// </summary>
        private const char TileG = '☻';
        /// <summary>
        /// Max allowed level for iterative deepening.
        /// </summary>
        private const int IterativeDeepeningMaxLevel = 100000;
        /// <summary>
        /// Min allowed level for iterative deepening.
        /// </summary>
        private const int IterativeDeepeningMinLevel = 1;
        /// <summary>
        /// How many milliseconds between 2 info updates.
        /// </summary>
        private const int UpdateInfoTimeFrameMilliSeconds = 500;
        /// <summary>
        /// Max allowed width/height for the game.
        /// </summary>
        private const int MaxWorldDimension = 7;

        #endregion

        #region Variables

        private Thread infoThread;
        private Thread searchThread;
        private GameState gameState = GameState.Stopped;
        private SearchType searchType = SearchType._1_DepthFirst;
        private char[,] initialState;
        private char[,] goalState;
        private char[][,] allStates;
        private int iterativeDeepeningLevel;
        private int worldWidth;
        private int worldHeight;
        /// <summary>
        /// Counter for the no. of nodes created.
        /// </summary>
        private long nodes = 0;
        /// <summary>
        /// Current level.
        /// </summary>
        private long levels = 0;
        /// <summary>
        /// If tile A is considered in the calcuations.
        /// </summary>
        private bool isTileAEnabled = true;
        /// <summary>
        /// If tile B is considered in the calcuations.
        /// </summary>
        private bool isTileBEnabled = true;
        /// <summary>
        /// If tile C is considered in the calcuations.
        /// </summary>
        private bool isTileCEnabled = true;
        /// <summary>
        /// Time stap when the search algorithm started.
        /// </summary>
        private DateTime startTime;
        /// <summary>
        /// Time span taken for processing the search algorithm.
        /// </summary>
        private TimeSpan processDuration;

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Load the game.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Initialize();
        }

        /// <summary>
        /// Start/pause the search according to the current state of the game.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            groupBoxSearchType.IsEnabled = false;
            groupBoxWorld.IsEnabled = false;
            string content = String.Empty;

            switch (gameState)
            {
                case GameState.Started:
                    Pause();
                    content = "►";
                    processDuration = DateTime.Now - startTime;
                    break;
                case GameState.Stopped:
                    Start();
                    content = "▐▐ ";
                    processDuration = TimeSpan.FromSeconds(0);
                    startTime = DateTime.Now;
                    break;
                case GameState.Paused:
                    Resume();
                    content = "▐▐ ";
                    startTime = DateTime.Now;
                    break;
                default:
                    break;
            }

            buttonStart.Content = content;
            labelGameState.Content = this.gameState.ToString().ToUpper();
        }

        /// <summary>
        /// Reset the search/game. Stop it if working.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonReset_Click(object sender, RoutedEventArgs e)
        {
            if (gameState == GameState.Started || gameState == GameState.Paused)
            {
                Reset();
                buttonStart.Content = "►";
                buttonStart.IsEnabled = true;
                groupBoxSearchType.IsEnabled = true;
                groupBoxWorld.IsEnabled = true;
                labelGameState.Content = this.gameState.ToString().ToUpper();
                labelLevels.Content = "ZERO";
                labelNodes.Content = "ZERO";
                textBoxOutput.Text = InitialOutput();
                labelTime.Content = string.Empty;

                startTime = DateTime.Now;
                processDuration = TimeSpan.FromSeconds(0);
            }
        }

        /// <summary>
        /// Change the type of the search.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            string tag = ((RadioButton)sender).Tag.ToString();
            int choise = int.Parse(tag);
            searchType = (SearchType)choise;
            gridLevel.IsEnabled = searchType == SearchType._3_DepthLimit || searchType == SearchType._4_BreadthLimit;
        }

        /// <summary>
        /// Increment the value of the textBoxLevel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonLevelUp_Click(object sender, RoutedEventArgs e)
        {
            if (iterativeDeepeningLevel < IterativeDeepeningMaxLevel - 1)
            {
                iterativeDeepeningLevel++;
                textBoxLevel.Text = iterativeDeepeningLevel.ToString();
                textBoxLevel.Tag = textBoxLevel.Text;
            }
        }

        /// <summary>
        /// Decrement the value of the textBoxLevel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonLevelDown_Click(object sender, RoutedEventArgs e)
        {
            if (iterativeDeepeningLevel > IterativeDeepeningMinLevel)
            {
                iterativeDeepeningLevel--;
                textBoxLevel.Text = iterativeDeepeningLevel.ToString();
                textBoxLevel.Tag = textBoxLevel.Text;
            }
        }

        /// <summary>
        /// Check if the entered value of the textBoxLevel is numeric.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBoxLevel_TextChanged(object sender, TextChangedEventArgs e)
        {
            int newValue;
            if (!int.TryParse(textBoxLevel.Text, out newValue) || newValue > IterativeDeepeningMaxLevel || newValue < 0)
            {
                textBoxLevel.Text = iterativeDeepeningLevel.ToString();
            }
            else
            {
                iterativeDeepeningLevel = newValue;
            }
        }

        /// <summary>
        /// Increase width of the buzzle block.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonWorldWidthUp_Click(object sender, RoutedEventArgs e)
        {
            if (worldWidth < MaxWorldDimension)
            {
                worldWidth++;
                textBoxWorldWidth.Text = worldWidth.ToString();
                textBoxWorldWidth.Tag = textBoxWorldWidth.Text;

                InitializeStatesInvoker();
            }
        }

        /// <summary>
        /// Decrease width of the buzzle block.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonWorldWidthDown_Click(object sender, RoutedEventArgs e)
        {
            if (worldWidth > 2)
            {
                worldWidth--;
                textBoxWorldWidth.Text = worldWidth.ToString();
                textBoxWorldWidth.Tag = textBoxWorldWidth.Text;

                InitializeStatesInvoker();
            }
        }

        /// <summary>
        /// Increase height of the buzzle block.
        /// </summary>
        private void ButtonWorldHeightUp_Click(object sender, RoutedEventArgs e)
        {
            if (worldHeight < MaxWorldDimension)
            {
                worldHeight++;
                textBoxWorldHeight.Text = worldHeight.ToString();
                textBoxWorldHeight.Tag = textBoxWorldHeight.Text;

                InitializeStatesInvoker();
            }
        }

        /// <summary>
        /// Decrease height of the buzzle block.
        /// </summary>
        private void ButtonWorldHeightDown_Click(object sender, RoutedEventArgs e)
        {
            if (worldHeight > 2)
            {
                worldHeight--;
                textBoxWorldHeight.Text = worldHeight.ToString();
                textBoxWorldHeight.Tag = textBoxWorldHeight.Text;

                InitializeStatesInvoker();
            }
        }

        /// <summary>
        /// Checkbox checked/unchecked, enable/disable the tiles A, B or C accordingly.
        /// </summary>
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkbox = (CheckBox)sender;
            string tag = checkbox.Tag.ToString();
            switch (tag)
            {
                case "A":
                    isTileAEnabled = checkbox.IsChecked ?? false;
                    break;

                case "B":
                    isTileBEnabled = checkbox.IsChecked ?? false;
                    break;

                case "C":
                    isTileCEnabled = checkbox.IsChecked ?? false;
                    break;

                default:
                    break;
            }

            InitializeStatesInvoker();
        }

        /// <summary>
        /// When closing, make sure everything is stopped/disposed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Reset();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initialize the game.
        /// </summary>
        private void Initialize()
        {
            iterativeDeepeningLevel = 1;
            worldWidth = 3;
            worldHeight = 3;

            textBoxLevel.Text = iterativeDeepeningLevel.ToString();
            textBoxWorldWidth.Text = worldWidth.ToString();
            textBoxWorldHeight.Text = worldHeight.ToString();

            startTime = DateTime.Now;
            processDuration = TimeSpan.FromSeconds(0);

            InitializeStatesInvoker();
        }

        /// <summary>
        /// Initialize initial, goal and empty states according to width and length of the game.
        /// </summary>
        private void InitializeStatesInvoker()
        {
            groupBoxWorld.IsEnabled = false;

            Thread thread = new Thread(new ThreadStart(InitializeStates));
            thread.Start();
        }

        /// <summary>
        /// Initialize initial, goal and empty states according to width and length of the game.
        /// </summary>
        private void InitializeStates()
        {
            // empty state
            char[,] emptyState = new char[this.worldHeight, this.worldWidth];
            for (int y = 0; y < worldHeight; y++)
            {
                for (int x = 0; x < worldWidth; x++)
                {
                    emptyState[y, x] = TileE;
                }
            }

            // initial state and goal state are created randomly
            initialState = RandomState(emptyState);
            goalState = RandomState(emptyState);

            // copy the goal state to another 1D array
            allStates = DataPermutations(TileG, emptyState);
            if (isTileAEnabled)
            {
                allStates = DataPermutations(TileA, allStates);
            }
            if (isTileBEnabled)
            {
                allStates = DataPermutations(TileB, allStates);
            }
            if (isTileCEnabled)
            {
                allStates = DataPermutations(TileC, allStates);
            }

            Dispatcher.Invoke(() =>
            {
                // all possible states
                labelPossibleStates.Content = String.Format("{0:0,0}", allStates.Length);

                // show the generated states to the user
                textBoxOutput.Text =
                    !EqualStates(initialState, goalState)
                    ? InitialOutput()
                    : "Randomly generated initial and goal states are equal, sorry!";

                groupBoxWorld.IsEnabled = true;
            });
        }

        /// <summary>
        /// Take empty state and fill it randomly with tiles A, B, C and G.
        /// </summary>
        private char[,] RandomState(char[,] emptyState)
        {
            char[,] state = (char[,])emptyState.Clone();

            List<char> tileList = new List<char>();
            tileList.Add(TileG);
            if (isTileAEnabled)
            {
                tileList.Add(TileA);
            }
            if (isTileBEnabled)
            {
                tileList.Add(TileB);
            }
            if (isTileCEnabled)
            {
                tileList.Add(TileC);
            }
            char[] tiles = tileList.ToArray();

            Random random = new Random();
            int max = worldHeight * worldWidth;

            for (int i = 0; i < tiles.Length; i++)
            {
                bool isTileAdded = false;
                while (!isTileAdded)
                {
                    Thread.Sleep(10);
                    double r = random.NextDouble() * max;
                    int index = (int)r;
                    int x = index % worldWidth;
                    int y = index / worldWidth;

                    if (state[y, x] == TileE)
                    {
                        state[y, x] = tiles[i];
                        isTileAdded = true;
                    }
                }
            }
            return state;
        }

        /// <summary>
        /// Start the game.
        /// </summary>
        private void Start()
        {
            ThreadStart threadStart;
            switch (searchType)
            {
                case SearchType._1_DepthFirst:
                    threadStart = new ThreadStart(DepthFirst);
                    break;
                case SearchType._2_BreadthFirst:
                    threadStart = new ThreadStart(BreadthFirst);
                    break;
                case SearchType._3_DepthLimit:
                    threadStart = new ThreadStart(DepthLimit);
                    break;
                case SearchType._4_BreadthLimit:
                    threadStart = new ThreadStart(BreadthLimit);
                    break;
                case SearchType._5_Heuristic:
                    threadStart = new ThreadStart(Heuristic);
                    break;
                case SearchType._6_IterativeDeepening:
                    threadStart = new ThreadStart(IterativeDeepening);
                    break;
                default:
                    threadStart = null;
                    string message = String.Format("Un-expected Search Type in switch case in Start() method. Search Type: {0}", searchType);
                    throw new ArgumentOutOfRangeException(message);
                    break;
            }

            // search thread
            searchThread = new Thread(threadStart);
            searchThread.Start();

            // info thread to update info every 1 seconds
            infoThread = new Thread(UpdateInfoInvoker);
            infoThread.Start();

            gameState = GameState.Started;
        }

        /// <summary>
        /// Resume the game (search for solution).
        /// </summary>
        private void Resume()
        {
            gameState = GameState.Started;
            searchThread.Resume();
            infoThread.Resume();
        }

        /// <summary>
        /// Pause the game (search for solution).
        /// </summary>
        private void Pause()
        {
            gameState = GameState.Paused;
            searchThread.Suspend();
            infoThread.Suspend();
        }

        /// <summary>
        /// Reset the game.
        /// </summary>
        private void Reset()
        {
            if (searchThread != null && searchThread.IsAlive)
            {
                if (searchThread.ThreadState == ThreadState.Suspended)
                {
                    searchThread.Resume();
                }
                searchThread.Abort();
            }
            if (infoThread != null && infoThread.IsAlive)
            {
                if (infoThread.ThreadState == ThreadState.Suspended)
                {
                    infoThread.Resume();
                }
                infoThread.Abort();
            }
            gameState = GameState.Stopped;

            // the counters need to be reseted after aborting the search thread
            nodes = 0;
            levels = 0;
        }

        /// <summary>
        /// Keep calling the UpdateInfo method every amout of time.
        /// </summary>
        private void UpdateInfoInvoker()
        {
            while (true)
            {
                Thread.Sleep(UpdateInfoTimeFrameMilliSeconds);
                this.Dispatcher.Invoke(UpdateInfo);
            }
        }

        /// <summary>
        /// Update some info on the interface: State, num. of levels and num. of nodes.
        /// </summary>
        private void UpdateInfo()
        {
            labelLevels.Content = String.Format("{0:0,0}", levels);
            labelNodes.Content = String.Format("{0:0,0}", nodes);

            TimeSpan duration = ((DateTime.Now - startTime) + processDuration);
            labelTime.Content = String.Format("{0:00}:{1:00}", duration.TotalMinutes, duration.Seconds);
        }

        /// <summary>
        /// The initial info to be displayed in the output box. Contains the initial and the goal states.
        /// </summary>
        /// <returns></returns>
        private string InitialOutput()
        {
            return string.Format("Initial State:{0}\n\n\nGoal State:{1}", FormatNodeData(initialState), FormatNodeData(goalState));
        }

        /// <summary>
        /// Print the solution restrosepctively.
        /// </summary>
        /// <param name="node"></param>
        private void PrintSolution(Node node)
        {
            int steps = 0;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("\n--------------------------------");
            stringBuilder.Append("\n| Hoooooooray, solution found! |");
            stringBuilder.Append("\n| The solution retrospectively |");
            stringBuilder.Append("\n--------------------------------");
            while (node != null)
            {
                steps++;
                stringBuilder.Append(FormatNodeData(node.Data));
                node = node.Parent;
            }

            stringBuilder.Append(String.Format("\n\nSTEPS: {0}\n\n---------------\n|     EOP     |\n---------------", steps));

            this.Dispatcher.Invoke(() =>
            {
                textBoxOutput.Text = stringBuilder.ToString();
            });
        }

        /// <summary>
        /// Print message inforimg the user that no solution was found.
        /// </summary>
        private void PrintSolutionNotFound()
        {
            string st = "\n--------------------------------\n|  Sorry, no solution found !  |\n--------------------------------";

            this.Dispatcher.Invoke(() =>
            {
                textBoxOutput.Text = string.Concat(textBoxOutput.Text, st);
            });
        }

        /// <summary>
        /// Build string out of given data.
        /// </summary>
        /// <param name="data"></param>
        private string FormatNodeData(char[,] data)
        {
            StringBuilder stringBuilder = new StringBuilder();

            int dimensionX = data.GetLength(1);
            int dimensionY = data.GetLength(0);

            stringBuilder.Append("\n");
            for (int i = 0; i < dimensionY; i++)
            {
                stringBuilder.Append("\n| ");

                for (int j = 0; j < dimensionX; j++)
                {
                    stringBuilder.Append(String.Format("{0} ", data[i, j]));
                }

                stringBuilder.Append("|");
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Stop the info and search thread. Update the status to 'Finish'.
        /// </summary>
        private void SearchFinished()
        {
            // after reaching a solution, stop the threads
            if (infoThread.ThreadState == ThreadState.Suspended)
            {
                infoThread.Resume();
            }
            infoThread.Abort();

            // display the latest info
            this.Dispatcher.Invoke(() =>
            {
                buttonStart.IsEnabled = false;
                labelGameState.Content = "FINISHED";
                UpdateInfo();
            });
        }

        /// <summary>
        /// Are the given states equal or not.
        /// </summary>
        /// <param name="dataA"></param>
        /// <param name="dataB"></param>
        /// <returns></returns>
        private bool EqualStates(char[,] dataA, char[,] dataB)
        {
            int dimensionX = dataA.GetLength(1);
            int dimensionY = dataA.GetLength(0);

            for (int i = 0; i < dimensionY; i++)
            {
                for (int j = 0; j < dimensionX; j++)
                {
                    if (dataA[i, j] != dataB[i, j])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Are the given states adjacent or not (adjacent states means one can move from one to another in just step).
        /// </summary>
        /// <returns></returns>
        private bool AdjacentStates(char[,] dataA, char[,] dataB)
        {
            if (EqualStates(dataA, dataB))
            {
                return false;
            }

            int dimensionX = dataA.GetLength(1);
            int dimensionY = dataA.GetLength(0);
            Index indexA;
            Index indexB;

            for (int i = 0; i < dimensionY; i++)
            {
                for (int j = 0; j < dimensionX; j++)
                {
                    if (dataA[i, j] != dataB[i, j])
                    {
                        int distanceTileG = DistanceTileG(dataA, dataB);
                        if (distanceTileG > 1)
                        {
                            return false;
                        }

                        int distanceExceptTileG = DistanceExceptTileG(dataA, dataB);
                        if (distanceExceptTileG > 1)
                        {
                            return false;
                        }

                        if (distanceTileG == 1 && distanceExceptTileG == 1)
                        {
                            // check if g did the swap
                            indexA = ArrayIndexOf(dataA, TileG);
                            indexB = ArrayIndexOf(dataB, TileG);
                            return dataB[indexA.Y, indexA.X] == dataA[indexB.Y, indexB.X] && dataB[indexA.Y, indexA.X] != TileE;
                        }
                        else if (distanceTileG == 1 && distanceExceptTileG == 0)
                        {
                            return true;
                        }

                        return false;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Build list of data filled with the given tile.
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        private char[][,] DataPermutations(char tile, params char[][,] states)
        {
            List<char[,]> permutatedStates = new List<char[,]>();
            char[,] newState = null;
            int dimensionX = 0;
            int dimensionY = 0;

            foreach (char[,] state in states)
            {
                dimensionX = state.GetLength(1);
                dimensionY = state.GetLength(0);

                for (int i = 0; i < dimensionY; i++)
                {
                    for (int j = 0; j < dimensionX; j++)
                    {
                        if (state[i, j] == TileE)
                        {
                            newState = (char[,])state.Clone();
                            newState[i, j] = tile;
                            permutatedStates.Add(newState);
                        }
                    }
                }
            }

            return permutatedStates.ToArray();
        }

        /// <summary>
        /// Get possible children for the given node. Possible means all possible nodes we can get
        /// if we move from the current node minus any ancestor/predecessor node with similar date.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private char[][,] PossibleChildren(Node node)
        {
            // expand all posibilities
            List<char[,]> childrenData = AllPossibleChildren(node.Data).ToList();

            // remove any child that is similar to one of its ancestors (predecessors)
            Node parentNode = node.Parent;
            while (parentNode != null)
            {
                int index = childrenData.FindIndex(i => EqualStates(i, parentNode.Data));
                if (index > -1)
                {
                    childrenData.RemoveAt(index);
                }
                parentNode = parentNode.Parent;
            }

            return childrenData.ToArray();
        }

        /// <summary>
        /// Calculate how far the agent tile 'TileG' in the given data is far from its goal position.
        /// </summary>
        /// <param name="dataA"></param>
        /// <param name="dataB"></param>
        /// <returns></returns>
        private int DistanceTileG(char[,] dataA, char[,] dataB)
        {
            int dimensionX = dataA.GetLength(1);
            int dimensionY = dataA.GetLength(0);
            int distance = 0;

            for (int i = 0; i < dimensionY; i++)
            {
                for (int j = 0; j < dimensionX; j++)
                {
                    // check if it is the agent tile 'G'
                    if (dataA[i, j] == TileG)
                    {
                        if (dataA[i, j] != dataB[i, j])
                        {
                            Index index = ArrayIndexOf(dataB, TileG);
                            distance = Math.Abs(i - index.Y) + Math.Abs(j - index.X);
                        }

                        return distance;
                    }
                }
            }
            return distance;
        }

        /// <summary>
        /// Calculate the distance between two states (take into consideration all tiles except tile G).
        /// </summary>
        /// <param name="dataA"></param>
        /// <param name="dataB"></param>
        /// <returns></returns>
        private int DistanceExceptTileG(char[,] dataA, char[,] dataB)
        {
            int dimensionX = dataA.GetLength(1);
            int dimensionY = dataA.GetLength(0);
            Index index = new Index(-1, -1);
            int distance = 0;

            for (int i = 0; i < dimensionY; i++)
            {
                for (int j = 0; j < dimensionX; j++)
                {
                    // check if tile is of interset, i.e Tile 'A', 'B', 'C'
                    // and the tile is mis-placed, then calculate distance of mis-placement
                    // (i.e how far this tile is way from the correct position/place).
                    if ((dataA[i, j] == TileA || dataA[i, j] == TileB || dataA[i, j] == TileC)
                        && dataA[i, j] != dataB[i, j])
                    {
                        index = ArrayIndexOf(dataB, dataA[i, j]);
                        distance += Math.Abs(i - index.Y) + Math.Abs(j - index.X);
                    }
                }
            }

            return distance;
        }

        /// <summary>
        /// Get all possible children for the given data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private char[][,] AllPossibleChildren(char[,] data)
        {
            char[][,] children = allStates.Where(i => AdjacentStates(i, data)).ToArray();
            return children;
        }

        /// <summary>
        /// Get x and y dimentions of the place of the given value in the given array.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private Index ArrayIndexOf(char[,] array, char value)
        {
            Index index = new Index(-1, -1);

            int dimensionX = array.GetLength(1);
            int dimensionY = array.GetLength(0);

            for (int i = 0; i < dimensionY; i++)
            {
                for (int j = 0; j < dimensionX; j++)
                {
                    if (array[i, j] == value)
                    {
                        index = new Index(i, j);
                        return index;
                    }
                }
            }
            return index;
        }

        #endregion

        #region Search Methods

        /// <summary>
        /// Depth first search algorithm.
        /// </summary>
        private void DepthFirst()
        {
            Node node = DepthFirst(0);

            bool isSolutionFound = EqualStates(node.Data, goalState);
            if (isSolutionFound)
            {
                PrintSolution(node);
            }
            else
            {
                PrintSolutionNotFound();
            }
            SearchFinished();
        }

        /// <summary>
        /// Depth first search algorithm until reaching the given maxLevel. If maxLevel = 0, there is
        /// no level limtation on the algorithm.
        /// </summary>
        private Node DepthFirst(int maxLevel = 0)
        {
            Node currentNode = new Node(initialState, null);
            char[][,] childrenData = null;
            nodes++;

            while (!EqualStates(currentNode.Data, goalState)
                && (maxLevel == 0 || (maxLevel > 0 && levels < maxLevel)))
            {
                nodes++;

                // mark the current node as visited/explored
                currentNode.Visited = true;

                // get possible children
                childrenData = PossibleChildren(currentNode);

                // if children found, move to the first one
                // else, move one step up
                if (childrenData.Length > 0)
                {
                    levels++;

                    // create child nodes for the currentNode
                    currentNode.Children = childrenData.Select(i => new Node(i, currentNode)).ToArray();

                    // set first child as the currentNode for the next step
                    currentNode = currentNode.Children[0];
                }
                else
                {
                    levels -= 2;

                    // if there are no childs, we need to go one level up
                    currentNode = DepthFirstStepUp(currentNode);
                    if (currentNode == null)
                    {
                        // "No 'Depth First' solution found at this level
                        currentNode = new Node(initialState, null);
                        break;
                    }
                }
            }

            return currentNode;
        }

        /// <summary>
        /// Go one step up for depth first algorithm.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private Node DepthFirstStepUp(Node node)
        {
            if (node.Parent == null)
            {
                return null;
            }

            // if the node has un-visited siblings, then return the first of them
            // else, call the same method recursively while passing the parent of the current node
            Node[] unVisitedSiblings = node.Parent.Children.Where(i => !i.Visited).ToArray();
            if (unVisitedSiblings.Length > 0)
            {
                return unVisitedSiblings[0];
            }
            else
            {
                return DepthFirstStepUp(node.Parent);
            }
        }

        /// <summary>
        /// Breadth first search algorithm.
        /// </summary>
        private void BreadthFirst()
        {
            BreadthFirst(0);
        }

        /// <summary>
        /// Breadth first search algorithm until reaching the given maxLevel. If maxLevel = 0, there is
        /// no level limtation on the algorithm.
        /// </summary>
        /// <param name="maxLevel"></param>
        private void BreadthFirst(int maxLevel = 0)
        {
            // input validation
            if (maxLevel < 0)
            {
                throw new ArgumentOutOfRangeException("Sorry, max level given to BreadthFirst() method can't be negative.");
            }

            Node initialNode = new Node(initialState, null);
            Level currentLevel = new Level(initialNode);
            List<Node> newLevelNodes = null;
            char[][,] newLevelData = null;

            // while the current level does not has the goal node,
            // move to the next level
            while (currentLevel.Nodes.Count(i => EqualStates(i.Data, goalState)) == 0
                && (maxLevel == 0 || (maxLevel > 0 && levels < maxLevel)))
            {
                levels++;
                newLevelNodes = new List<Node>();
                foreach (Node node in currentLevel.Nodes)
                {
                    newLevelData = PossibleChildren(node);
                    newLevelNodes.AddRange(newLevelData.Select(i => new Node(i, node)));

                    nodes += newLevelData.Length;
                }

                // create the new level
                currentLevel = new Level(newLevelNodes.ToArray());
            }

            // check if solution was found
            Node goalNode = currentLevel.Nodes.FirstOrDefault(i => EqualStates(i.Data, goalState));
            bool isSolutionFound = goalNode != null;
            if (isSolutionFound)
            {
                PrintSolution(goalNode);
            }
            else
            {
                PrintSolutionNotFound();
            }
            SearchFinished();
        }

        /// <summary>
        /// Depth limit search algorithm with the maxLevel set by user
        /// using UI (using variable iterativeDeepeningLevel).
        /// </summary>
        private void DepthLimit()
        {
            DepthFirst(iterativeDeepeningLevel);
        }

        /// <summary>
        /// Breadth limit search algorithm with the maxLevel set by user
        /// using UI (using variable iterativeDeepeningLevel).
        /// </summary>
        private void BreadthLimit()
        {
            BreadthFirst(iterativeDeepeningLevel);
        }

        /// <summary>
        /// Heuristic search algorithm.
        /// </summary>
        private void Heuristic()
        {
            Node currentNode = new Node(initialState, null);
            char[][,] childrenData = null;
            int[] heuresticValues;
            int indexOfMinValue;
            nodes++;

            while (!EqualStates(currentNode.Data, goalState))
            {
                nodes++;
                levels++;

                // get possible children
                childrenData = PossibleChildren(currentNode);
                if (childrenData.Length == 0)
                {
                    // hooooray, the algo 'Heuristic' failed! to find a solution
                    break;
                }

                // calculate the heurestic for all of them
                heuresticValues = new int[childrenData.Length];
                for (int i = 0; i < heuresticValues.Length; i++)
                {
                    heuresticValues[i] = HeuristicValue(childrenData[i], goalState);
                }

                // find the data with the min heurestic cost/value
                // and set it as the next node
                int minValue = heuresticValues.Min();
                indexOfMinValue = heuresticValues.ToList().IndexOf(minValue);
                currentNode = new Node(childrenData[indexOfMinValue], currentNode);
            }

            if (EqualStates(currentNode.Data, goalState))
            {
                PrintSolution(currentNode);
            }
            else
            {
                PrintSolutionNotFound();
            }
            SearchFinished();
        }

        /// <summary>
        /// Calculate the heuristic value/cost for the given data.
        /// The heuristic value is the cost from state one to state two.
        /// </summary>
        /// <param name="dataA"></param>
        /// <returns></returns>
        private int HeuristicValue(char[,] dataA, char[,] dataB)
        {
            // there are several ways to calculate the heurestic value
            // approach 1: number of mis-placed tiles
            // approach 2: total manhatten distance (total means cal distance for each tile)
            // approach 3: any other distance algorithm

            // approach 1
            int dimensionX = dataA.GetLength(1);
            int dimensionY = dataA.GetLength(0);
            int misplacedCount = 0;
            int distance = 0;
            Index index = new Index(-1, -1);

            for (int i = 0; i < dimensionY; i++)
            {
                for (int j = 0; j < dimensionX; j++)
                {
                    // check if tile is of interset, i.e Tile 'A', 'B', 'C' or 'G'
                    // and the tile is mis-placed, then calculate distance of mis-placement
                    // (i.e how far this tile is way from the correct position/place).
                    if ((dataA[i, j] == TileA
                        || dataA[i, j] == TileB
                        || dataA[i, j] == TileC
                        || dataA[i, j] == TileG)
                        && dataA[i, j] != goalState[i, j])
                    {
                        index = ArrayIndexOf(dataB, dataA[i, j]);
                        distance = Math.Abs(i - index.Y) + Math.Abs(j - index.X);
                        misplacedCount += distance;
                    }
                }
            }

            return misplacedCount;
        }

        /// <summary>
        /// Iterrative deepening search algorithm.
        /// </summary>
        private void IterativeDeepening()
        {
            int maxLevel = 1;
            bool isSolutionFound = false;
            Node node = null;

            while (!isSolutionFound)
            {
                nodes = 0;
                levels = 0;
                node = DepthFirst(maxLevel++);
                isSolutionFound = EqualStates(node.Data, goalState);
            }

            PrintSolution(node);
            SearchFinished();
        }

        /// <summary>
        /// Used to just test the start/pause/reset functionality and if the threads are working fine.
        /// </summary>
        private void DummySearch()
        {
            decimal x = 1;
            for (long i = 0; i < 100000000; i++)
            {
                x++;
                x--;
                Math.Round(x);
                nodes++;
                if (nodes % 127 == 0)
                {
                    levels++;
                }
            }

            PrintSolutionNotFound();
            SearchFinished();
        }

        #endregion
    }
}