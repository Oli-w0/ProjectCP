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
    
    bool isGameOver = false;
    
    // Constructor: Called when the page is created
    public MainPage()
    {
        // Initialize all XAML components (UI elements)
        InitializeComponent();

        // Subscribe to SizeChanged event to wait until the layout has a real size
        // This ensures Game.Width and Game.Height have valid values before we use them
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
        // Adjust these numbers to match your road image
        double leftMargin = Game.Width * 0.16;   // 16% margin on left side
        double rightMargin = Game.Width * 0.16;  // 16% margin on right side
        double usableWidth = Game.Width - leftMargin - rightMargin; // Road width
        double laneWidth = usableWidth / 3; // Width of each lane

        // Offset for middle lane if road is slightly off-center
        double middleOffset = Game.Width * 0.08;

        // Calculate the center X-coordinate of each lane
        laneCenters = new double[]
        {
            leftMargin + laneWidth / 2,                    // Left lane center
            leftMargin + laneWidth * 1.5 + middleOffset,   // Middle lane center
            leftMargin + laneWidth * 2.5                   // Right lane center
        };

        // Position the player in the middle lane, near the bottom of the screen
        double playerY = Game.Height - 150; // 150 pixels from bottom
        
        // Set player position: X centered in middle lane, Y near bottom
        // Rect parameters: (X, Y, Width, Height)
        AbsoluteLayout.SetLayoutBounds(Player, new Rect(laneCenters[1] - 15, playerY, 30, 80));

        // NOW it's safe to set up player movement (Game has valid dimensions)
        movePlayer(Player);

        // Start spawning obstacles every 1 second
        Device.StartTimer(TimeSpan.FromSeconds(1), SpawnObstacle);
    }

    // Timer callback: Spawns a new obstacle car in a random lane
    // Returns true to keep the timer running
    bool SpawnObstacle()
    {
        // Safety check: make sure lanes have been calculated
        if (laneCenters == null) return true;

        // Create a new obstacle image (enemy car)
        var obstacle = new Image
        {
            Source = "obstacle_car.png",
            WidthRequest = 200,  // Requested width (may be scaled)
            HeightRequest = 200, // Requested height (may be scaled)
            Aspect = Aspect.AspectFit // Maintain aspect ratio
        };

        // Pick a random lane (0 = left, 1 = middle, 2 = right)
        int lane = random.Next(0, 3);
        
        // Calculate X position to center the car in the chosen lane
        double x = laneCenters[lane] - 25; // Offset by half the car width
        
        // Start the obstacle above the screen (negative Y)
        double y = -100;

        // Set the obstacle's position and size
        AbsoluteLayout.SetLayoutBounds(obstacle, new Rect(x, y, 50, 80));
        
        // Add the obstacle to the game layout (makes it visible)
        Game.Children.Add(obstacle);

        // Start moving the obstacle down the screen
        MoveObstacle(obstacle);
        
        if(isGameOver) return false;
        
        // Return true to keep the timer running (spawn more obstacles)
        return true;
        
    }

    // Continuously moves an obstacle down the screen
    async void MoveObstacle(Image obstacle)
    {

        // Keep moving while the obstacle is still on screen
        while (AbsoluteLayout.GetLayoutBounds(obstacle).Y < Game.Height)
        {
            // Get current position
            var bounds = AbsoluteLayout.GetLayoutBounds(obstacle);
            bounds.Y += 6;
            AbsoluteLayout.SetLayoutBounds(obstacle, bounds);

            var playerRect = GetHitbox(Player);
            var obstacleRect = GetHitbox(obstacle);
            
            // Move down by 6 pixels
            bounds.Y += 6;
            
            // Update position
            AbsoluteLayout.SetLayoutBounds(obstacle, bounds);
            if (playerRect.IntersectsWith(obstacleRect))
            {
                isGameOver = true;
                DisplayAlert("", "Game Over!", "OK");
                return;
            }
            
            // Wait ~16ms (approximately 60 FPS)
            await Task.Delay(16);
           
        }

        // Obstacle has moved off screen - remove it to free memory
        if (!isGameOver)
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
                    // Math.Clamp ensures value stays between min and max
                    _currentx = Math.Clamp(_currentx, 0, Game.Width - Player.Width);

                    // Get current position (we need to keep the Y value unchanged)
                    var currentBounds = AbsoluteLayout.GetLayoutBounds(Player);
                    
                    // Update position: new X, same Y, same width/height
                    AbsoluteLayout.SetLayoutBounds(Player,
                        new Rect(_currentx, currentBounds.Y, currentBounds.Width, currentBounds.Height));
                    break;
            }
        };
        if(isGameOver) return; //block movement

        // Add the gesture recognizer to the player so it responds to touch
        Player.GestureRecognizers.Add(panGesture);
    }
    
    // STEP 3 — Convert any MAUI view into a rectangle (hitbox)
    Rect GetHitbox(Image img)
    {
        // Get the current layout bounds of the image (its position + size)
        var bounds = AbsoluteLayout.GetLayoutBounds(img);

        // Build and return a rectangle using those values
        return new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height);
    }

}

//To DO
//[* = priority] [() = possibility] [✅ = done]
//Get obstacle cars spawning ✅
//Change blocks to car images ✅
//Allow user car to be movable ✅
//Add game over for colliding with obstacle*
//Add a UI*
//Add special items*
//Add different vehicles in a shop menu
//Add sounds*
//Make it more difficult the longer the user goes*
//Fix the obstacle spawning*
//Add custom maps()
//Menu music()*
//Keep score of coins and distance*
//Make moving animations smooth*
//Allow creation of new users
//


