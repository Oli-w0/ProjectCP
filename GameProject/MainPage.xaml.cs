namespace GameProject;
    public partial class MainPage : ContentPage
    {
        double[] laneCenters;
        Random random = new Random();
       

        public MainPage()
        {
            InitializeComponent();

            // Wait until the layout has a real size
            this.SizeChanged += OnGameSizeChanged;
        }

        void OnGameSizeChanged(object sender, EventArgs e)
        {
            if (Game.Width <= 0) return; // safety check
            this.SizeChanged -= OnGameSizeChanged; // run once

            // Adjust these numbers to match your road image
            double leftMargin = Game.Width * 0.16;   // space between screen edge and left lane
            double rightMargin = Game.Width * 0.16;  // space between screen edge and right lane
            double usableWidth = Game.Width - leftMargin - rightMargin;
            double laneWidth = usableWidth / 3;

            double middleOffset = Game.Width * 0.08;

            laneCenters = new double[]
            {
                leftMargin + laneWidth / 2,          // left lane center
                leftMargin + laneWidth * 1.5 + middleOffset,        // middle lane center
                leftMargin + laneWidth * 2.5         // right lane center
            };

            // Place the player in the middle lane initially
            double playerY = Game.Height - 150;
            AbsoluteLayout.SetLayoutBounds(Player, new Rect(laneCenters[1] - 10, playerY, 30, 80));

            // Start spawning obstacles
            Device.StartTimer(TimeSpan.FromSeconds(1), SpawnObstacle);
        }

        bool SpawnObstacle()
        {
            if (laneCenters == null) return true;

            var obstacle = new Image
            {
                Source = "obstacle_car.png",
                WidthRequest = 120,
                HeightRequest = 120,
                Aspect = Aspect.AspectFit
            };

            // Pick random lane (0–2)
            int lane = random.Next(0, 3);
            double x = laneCenters[lane] - 25; // center car
            double y = -100;

            AbsoluteLayout.SetLayoutBounds(obstacle, new Rect(x, y, 50, 80));
            Game.Children.Add(obstacle);

            MoveObstacle(obstacle);
            return true;
        }

        async void MoveObstacle(Image obstacle)
        {
            while (AbsoluteLayout.GetLayoutBounds(obstacle).Y < Game.Height)
            {
                var bounds = AbsoluteLayout.GetLayoutBounds(obstacle);
                bounds.Y += 6;
                AbsoluteLayout.SetLayoutBounds(obstacle, bounds);
                await Task.Delay(16);
            }
            Game.Children.Remove(obstacle);
        }

       

    }

//To DO
//[* = priority] [() = possibility] [✅ = done]
//Get obstacle cars spawning ✅
//Change blocks to car images ✅
//Allow user car to be movable*
//Add game over for colliding with obstacle*
//Add a UI*
//Add special items*
//Add different vehicles in a shop menu
//Add sounds*
//Make it more difficult the longer the user goes*
//Fix the obstacle spawning*
//Add custom maps()
//Allow users to import images ()
//Menu music()*
//Keep score of coins and distance*
//Make moving animations smooth*
//Allow creation of new users
//


