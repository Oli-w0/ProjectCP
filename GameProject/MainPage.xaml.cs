namespace GameProject;

public partial class MainPage : ContentPage
{
    // Array to store the center X-coordinate of each lane (left, left-middle, right-middle, right)
    double[] laneCenters;

    // Random number generator for spawning obstacles in random lanes
    Random random = new Random();

    // Tracks the player's starting X position when pan gesture begins
    private double _startx;

    // Tracks the player's current X position during pan gesture
    private double _currentx;

    // Flag to check if game is currently running
    bool isGameOver = false;
    bool isGameStarted = false;
    bool isPaused = false;

    // Distance traveled (acts as score)
    private double _distanceTravelled = 0;

    // High score tracking (persists between games)
    private double _highScore = 0;

    // Difficulty variables
    private double obstacleSpeed;
    private double spawnInterval;
    
    private double leftRoadEdge;
    private double rightRoadEdge;
    
    // Constructor: Called when the page is created
    public MainPage()
    {
        // Initialize all XAML components (UI elements)
        InitializeComponent();

        // Load high score from preferences
        _highScore = Preferences.Default.Get("HighScore", 0.0);
        StartScreenHighScore.Text = $"High Score: {Math.Floor(_highScore)}";

        // Subscribe to SizeChanged event to wait until the layout has a real size
        this.SizeChanged += OnGameSizeChanged;
    }

    // Event handler that runs when the page size changes (after layout is measured)
    void OnGameSizeChanged(object sender, EventArgs e)
    {
        // Safety check: make sure the layout has been measured
        if (Game.Width <= 0) return;

        // Unsubscribe from the event so this only runs once
        this.SizeChanged -= OnGameSizeChanged;

        // Calculate lane positions based on screen width
        double leftMargin = Game.Width * 0.16;   // 16% space on left
        double rightMargin = Game.Width * 0.16;  // 16% space on right
        double usableWidth = Game.Width - leftMargin - rightMargin; // Road width
        double laneWidth = usableWidth / 4; // Divide road into 4 lanes

        // Set road boundaries for player movement
        leftRoadEdge = leftMargin;
        rightRoadEdge = Game.Width - rightMargin;

        // Calculate the center X-coordinate of each of the 4 lanes
        laneCenters = new double[]
        {
            leftMargin + laneWidth * 0.5,  // Lane 0: Left lane center
            leftMargin + laneWidth * 1.5,  // Lane 1: Left-middle lane center
            leftMargin + laneWidth * 2.5,  // Lane 2: Right-middle lane center
            leftMargin + laneWidth * 3.5   // Lane 3: Right lane center
        };

        // Position the player in the second lane (left-middle), near the bottom
        double playerY = Game.Height - 150;
        AbsoluteLayout.SetLayoutBounds(Player, new Rect(laneCenters[1] - 15, playerY, 30, 80));

        // Set up player movement controls
        movePlayer(Player);
    }

    // Start button clicked - begin the game
    private void StartGame_Clicked(object? sender, EventArgs e)
    {
        // Hide start screen
        StartScreen.IsVisible = false;

        // Reset game state
        isGameOver = false;
        isGameStarted = true;
        isPaused = false;
        _distanceTravelled = 0;
        obstacleSpeed = 10;
        spawnInterval = 0.7;
        
        // Clear any existing obstacles
        ClearObstacles();

        // Start the score counter (updates every ~16ms for 60 FPS)
        Device.StartTimer(TimeSpan.FromMilliseconds(16), () =>
        {
            // If paused, keep timer alive but don't update score
            if (isPaused)
                return true;

            // Stop if game is over
            if (isGameOver || !isGameStarted)
                return false;

            // Increment distance (acts as score)
            _distanceTravelled += 0.15;
            ScoreLabel.Text = $"Score: {Math.Floor(_distanceTravelled)}";

            return true; // Keep timer running
        });

        // Start spawning obstacles
        Device.StartTimer(TimeSpan.FromSeconds(spawnInterval), SpawnObstacle);
    }

    // Spawn a new obstacle in a random lane
    bool SpawnObstacle()
    {
        // If paused, keep timer alive but don't spawn
        if (isPaused)
            return true;

        // Safety checks
        if (laneCenters == null || isGameOver || !isGameStarted)
            return false;

        // Create a new obstacle image (enemy car)
        var obstacle = new Image
        {
            Source = "obstacle_car.png",
            WidthRequest = 180,
            HeightRequest = 180,
            Aspect = Aspect.AspectFit
        };

        // Pick a random lane from 0-3 (4 lanes total)
        int lane = random.Next(0, 4);

        // Calculate X position to center the car in the chosen lane
        double x = laneCenters[lane] - 25; // Offset to center the car

        // Start above the screen (negative Y means above visible area)
        double y = -100;

        // Set position and size
        AbsoluteLayout.SetLayoutBounds(obstacle, new Rect(x, y, 50, 80));

        // Add to game layout (makes it visible)
        Game.Children.Add(obstacle);

        // Start moving it down the screen
        MoveObstacle(obstacle);

        // Update difficulty based on current score
        UpdateDifficulty();

        // Return true to keep this timer running
        return true;
    }

    // Continuously moves an obstacle down the screen
    async void MoveObstacle(Image obstacle)
    {
        // Keep moving while obstacle is on screen and game is running
        while (AbsoluteLayout.GetLayoutBounds(obstacle).Y < Game.Height && !isGameOver)
        {
            // If paused, wait and skip this frame
            if (isPaused)
            {
                await Task.Delay(50);
                continue;
            }

            // Get current position
            var bounds = AbsoluteLayout.GetLayoutBounds(obstacle);

            // Move down by obstacle speed
            bounds.Y += obstacleSpeed;

            // Update position
            AbsoluteLayout.SetLayoutBounds(obstacle, bounds);

            // Get hitboxes for collision detection
            var playerRect = GetHitbox(Player);
            var obstacleRect = GetHitbox(obstacle);
            

            // Check if player and obstacle are overlapping
            if (playerRect.IntersectsWith(obstacleRect))
            {
                // Collision detected - game over!
                isGameOver = true;
                isGameStarted = false;
                await ShowGameOverUI();
                return;
            }

            // Wait ~16ms (approximately 60 FPS)
            await Task.Delay(16);
        }

        // Remove obstacle from screen when it goes off-screen or game ends
        if (Game.Children.Contains(obstacle))
        {
            Game.Children.Remove(obstacle);
        }
    }

    // Sets up pan gesture (swipe/drag) for moving the player left and right
    private void movePlayer(Image Player)
    {
        // Create a new pan gesture recognizer
        var panGesture = new PanGestureRecognizer();

        // Define what happens when the user pans (drags) their finger
        panGesture.PanUpdated += (s, e) =>
        {
            // Don't allow movement if game is over, not started, or paused
            if (isGameOver || !isGameStarted || isPaused)
                return;

            // Check the current status of the gesture
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    // User just touched the screen - remember starting position
                    _startx = AbsoluteLayout.GetLayoutBounds(Player).X;
                    break;

                case GestureStatus.Running:
                    // User is dragging - move the player horizontally
                    
                    // Calculate new X position based on drag distance
                    _currentx = _startx + e.TotalX;

                    // Clamp X position to keep player within road boundaries
                    _currentx = Math.Clamp(_currentx, leftRoadEdge, rightRoadEdge - Player.Width);

                    // Get current position (keep Y value unchanged)
                    var currentBounds = AbsoluteLayout.GetLayoutBounds(Player);

                    // Update position: new X, same Y, same width/height
                    AbsoluteLayout.SetLayoutBounds(Player,
                        new Rect(_currentx, currentBounds.Y, currentBounds.Width, currentBounds.Height));
                    break;
            }
        };

        // Add the gesture recognizer to the player
        Player.GestureRecognizers.Add(panGesture);
    }

    // Gets the collision box (hitbox) for an image
    Rect GetHitbox(Image img)
    {
        var bounds = AbsoluteLayout.GetLayoutBounds(img);
        return new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height);
    }

    // Increases difficulty as player progresses
    void UpdateDifficulty()
    {
        if (_distanceTravelled >= 1500)
        {
            obstacleSpeed = 20;
            spawnInterval = 0.3;
        }
        else if (_distanceTravelled >= 1000)
        {
            obstacleSpeed = 16;
            spawnInterval = 0.3;
        }
        else if (_distanceTravelled >= 600)
        {
            obstacleSpeed = 14;
            spawnInterval = 0.5;
        }
        else if (_distanceTravelled >= 300)
        {
            obstacleSpeed = 12;
            spawnInterval = 0.6;
        }
    }

    // Shows the game over screen with final score
    async Task ShowGameOverUI()
    {
        isGameOver = true;
        isGameStarted = false;
        isPaused = false;

        // Display final score
        FinalDistanceLabel.Text = $"Score: {Math.Floor(_distanceTravelled)}";

        // Check if new high score
        bool isNewHighScore = _distanceTravelled > _highScore;
        if (isNewHighScore)
        {
            _highScore = _distanceTravelled;
            // Save high score to device storage (persists between app launches)
            Preferences.Default.Set("HighScore", _highScore);
            NewHighScoreLabel.IsVisible = true;
        }
        else
        {
            NewHighScoreLabel.IsVisible = false;
        }

        // Display high score
        BestScoreLabel.Text = $"Best: {Math.Floor(_highScore)}";
        HighScoreLabel.Text = $"Best: {Math.Floor(_highScore)}";

        // Show game over panel with fade-in animation
        GameOverPanel.IsVisible = true;
        GameOverPanel.Opacity = 0;
        await GameOverPanel.FadeTo(1, 500);
    }

    // Restart button clicked - start a new game
    private void RestartGame_Clicked(object? sender, EventArgs e)
    {
        // Hide game over screen
        GameOverPanel.IsVisible = false;

        // Reset game state
        isGameOver = false;
        isGameStarted = true;
        isPaused = false;
        _distanceTravelled = 0;
        obstacleSpeed = 10;
        spawnInterval = 0.7;
        
        // Clear all obstacles from previous game
        ClearObstacles();

        // Reset player position to starting lane (lane 1 = left-middle)
        double playerY = Game.Height - 150;
        AbsoluteLayout.SetLayoutBounds(Player, new Rect(laneCenters[1] - 15, playerY, 30, 80));

        // Restart score counter
        Device.StartTimer(TimeSpan.FromMilliseconds(16), () =>
        {
            if (isPaused)
                return true;
                
            if (isGameOver || !isGameStarted)
                return false;

            _distanceTravelled += 0.15;
            ScoreLabel.Text = $"Score: {Math.Floor(_distanceTravelled)}";

            return true;
        });

        // Restart spawning obstacles
        Device.StartTimer(TimeSpan.FromSeconds(spawnInterval), SpawnObstacle);
    }

    // Main menu button clicked - return to start screen
    private void MainMenu_Clicked(object? sender, EventArgs e)
    {
        // Hide game over panel
        GameOverPanel.IsVisible = false;

        // Show start screen
        StartScreen.IsVisible = true;

        // Update high score display
        StartScreenHighScore.Text = $"High Score: {Math.Floor(_highScore)}";

        // Reset game state
        isGameOver = false;
        isGameStarted = false;
        isPaused = false;
        _distanceTravelled = 0;

        // Clear all obstacles
        ClearObstacles();

        // Reset player position
        double playerY = Game.Height - 150;
        AbsoluteLayout.SetLayoutBounds(Player, new Rect(laneCenters[1] - 15, playerY, 30, 80));
    }

    // Remove all obstacle cars from the screen
    void ClearObstacles()
    {
        // Create a list to store obstacles to remove
        var toRemove = new List<View>();

        // Find all obstacle images
        foreach (var child in Game.Children)
        {
            // Check if it's an Image and not the player
            if (child is Image img && img != Player)
            {
                // Check if it's an obstacle by looking at the source
                var source = img.Source?.ToString() ?? "";
                if (source.Contains("obstacle"))
                {
                    toRemove.Add(img);
                }
            }
        }

        // Remove all found obstacles
        foreach (var item in toRemove)
        {
            Game.Children.Remove(item);
        }
    }

    // Pause button clicked - toggle pause state
    void PauseButton_Clicked(object sender, EventArgs e)
    {
        // Toggle pause state
        isPaused = !isPaused;

        // Change button icon based on state
        // ⏸️ = pause icon (shown when game is playing)
        // ▶️ = play icon (shown when game is paused)
        PauseButton.Text = isPaused ? "▶️" : "⏸️";
    }
}