namespace GameProject;

public partial class MainPage : ContentPage
{
    // Array to store the center X-coordinate of each lane (left, middle, right)
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

    // Distance traveled (acts as score)
    private double _distanceTravelled = 0;

    // High score tracking (persists between games)
    private double _highScore = 0;

    // Difficulty variables
    double obstacleSpeed = 6;
    double spawnInterval = 1.0;

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
        double leftMargin = Game.Width * 0.16;
        double rightMargin = Game.Width * 0.16;
        double usableWidth = Game.Width - leftMargin - rightMargin;
        double laneWidth = usableWidth / 3;

        double middleOffset = Game.Width * 0.08;

        // Calculate the center X-coordinate of each lane
        laneCenters = new double[]
        {
            leftMargin + laneWidth / 2,                    // Left lane center
            leftMargin + laneWidth * 1.5 + middleOffset,   // Middle lane center
            leftMargin + laneWidth * 2.5                   // Right lane center
        };

        // Position the player in the middle lane, near the bottom
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
        _distanceTravelled = 0;
        obstacleSpeed = 6;
        spawnInterval = 1.0;
        
        //ClearObstacles();

        // Start the score counter (updates every frame)
        Device.StartTimer(TimeSpan.FromMilliseconds(16), () =>
        {
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
        // Safety checks
        if (laneCenters == null || isGameOver || !isGameStarted)
            return false;

        // Create a new obstacle image (enemy car)
        var obstacle = new Image
        {
            Source = "obstacle_car.png",
            WidthRequest = 170,
            HeightRequest = 170,
            Aspect = Aspect.AspectFit
        };

        // Pick a random lane (0 = left, 1 = middle, 2 = right)
        int lane = random.Next(0, 3);

        // Calculate X position to center the car in the chosen lane
        double x = laneCenters[lane] - 25;

        // Start above the screen
        double y = -100;

        // Set position and size
        AbsoluteLayout.SetLayoutBounds(obstacle, new Rect(x, y, 50, 80));

        // Add to game layout
        Game.Children.Add(obstacle);

        // Start moving it down
        MoveObstacle(obstacle);

        // Update difficulty based on current score
        UpdateDifficulty();

        // Schedule next spawn with current spawn interval
        Device.StartTimer(TimeSpan.FromSeconds(spawnInterval), SpawnObstacle);

        return false; // Don't repeat this specific timer (we create new ones)
    }

    // Continuously moves an obstacle down the screen
    async void MoveObstacle(Image obstacle)
    {
        // Keep moving while obstacle is on screen and game is running
        while (AbsoluteLayout.GetLayoutBounds(obstacle).Y < Game.Height && !isGameOver)
        {
            // Get current position
            var bounds = AbsoluteLayout.GetLayoutBounds(obstacle);

            // Move down by obstacle speed
            bounds.Y += obstacleSpeed;

            // Update position
            AbsoluteLayout.SetLayoutBounds(obstacle, bounds);

            // Check for collision with player
            var playerRect = GetHitbox(Player);
            var obstacleRect = GetHitbox(obstacle);

            if (playerRect.IntersectsWith(obstacleRect))
            {
                // Collision detected - game over!
                isGameOver = true;
                isGameStarted = false;
                await ShowGameOverUI();
                return;
            }

            // Wait ~16ms 
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
            // Don't allow movement if game is over or not started
            if (isGameOver || !isGameStarted)
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

                    // Clamp X position to keep player on screen
                    _currentx = Math.Clamp(_currentx, 0, Game.Width - Player.Width);

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
            obstacleSpeed = 16;
            spawnInterval = 0.3;
        }
        else if (_distanceTravelled >= 1000)
        {
            obstacleSpeed = 12;
            spawnInterval = 0.4;
        }
        else if (_distanceTravelled >= 600)
        {
            obstacleSpeed = 9;
            spawnInterval = 0.6;
        }
        else if (_distanceTravelled >= 300)
        {
            obstacleSpeed = 7;
            spawnInterval = 0.8;
        }
    }

    // Shows the game over screen with final score
    async Task ShowGameOverUI()
    {
        isGameOver = true;
        isGameStarted = false;

        // Display final score
        FinalDistanceLabel.Text = $"Score: {Math.Floor(_distanceTravelled)}";

        // Check if new high score
        bool isNewHighScore = _distanceTravelled > _highScore;
        if (isNewHighScore)
        {
            _highScore = _distanceTravelled;
            // Save high score to device storage
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
        _distanceTravelled = 0;
        obstacleSpeed = 6;
        spawnInterval = 1.0;
        
        //ClearObstacles();

        // Reset player position
        double playerY = Game.Height - 150;
        AbsoluteLayout.SetLayoutBounds(Player, new Rect(laneCenters[1] - 15, playerY, 30, 80));

        // Restart score counter
        Device.StartTimer(TimeSpan.FromMilliseconds(16), () =>
        {
            if (isGameOver || !isGameStarted)
                return false;

            _distanceTravelled += 0.15;
            ScoreLabel.Text = $"Score: {Math.Floor(_distanceTravelled)}";

            return true;
        });

        // Restart spawning
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
        _distanceTravelled = 0;

        // Clear obstacles
        ClearObstacles();

        // Reset player position
        double playerY = Game.Height - 150;
        AbsoluteLayout.SetLayoutBounds(Player, new Rect(laneCenters[1] - 15, playerY, 30, 80));
    }

    // Remove all obstacle cars from the screen
    void ClearObstacles()
    {
     
    }
}

