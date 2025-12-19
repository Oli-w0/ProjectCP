A fast-paced, 2D endless racing game built using .NET MAUI. Test your reflexes by weaving through traffic in a multi-lane highway. The game features dynamic scaling, difficulty progression, and high-score persistence.

 Features

    Endless Gameplay: The game speeds up as your score increases, challenging your reaction time.

    Touch & Gesture Controls: Smooth horizontal movement using PanGestureRecognizer.

    Score Tracking: Real-time score calculation with high-score saving via Maui.Storage.Preferences.

    Pause/Resume: Fully functional game state management.

 Tech Stack

    Framework: .NET MAUI

    Language: C#

    Layout: AbsoluteLayout for precise coordinate-based game object movement.

    Graphics: PNG-based sprites with AspectFill scaling.

 How to Play

    Launch the Game: Click the "Start Game" button from the main menu.

    Move your Car: * Desktop: Click and drag your car left or right.

    Avoid Obstacles: If you collide with an incoming car, it's Game Over!

    Pause: Use the ⏸️ button at the top left to take a break.

 Project Structure

    MainPage.xaml: Defines the game UI, including the game road, overlays (Start/Game Over), and labels.

    MainPage.xaml.cs: Contains the core game engine logic, collision detection, and layout math.

    Resources/Images: Contains car sprites (jbr_err_pid18957.png and obstacle_car.png) and the road background.

 Installation & Setup

    Clone this repository.

    Open the .sln file in Visual Studio 2022.

    Ensure you have the .NET MAUI workload installed.

    Select your target (Windows, Android, or iOS) and press F5 to run.
