using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Snake_soutez
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D _pixelTexture;
        private Random _random;

        // Game objects
        private List<Vector2> _snake;
        private List<Vector2> _foods;
        private bool _gameOver;
        private bool _victory;

        // Timing
        private float _timer;
        private const float MOVE_DELAY = 0.12f;

        // Game constants - 4x větší mapa
        private const int CELL_SIZE = 20;
        private const int GRID_WIDTH = 300;
        private const int GRID_HEIGHT = 108;
        private const int WIN_FOOD_COUNT = 15;
        private const int MAX_FOOD_COUNT = 40;
        private int _foodEaten = 0;

        // Vehicle controls
        private IVehicle _vehicle;
        private float _steeringWheel;
        private const float MAX_STEERING = 2.0f;
        private Keys[] _gearKeys = { Keys.D1, Keys.D2, Keys.D3 };

        // Obstacles
        private List<Vector2> _obstacles;
        private HashSet<Point> _obstacleHashSet;

        // Mouse steering
        private bool _isMouseDragging = false;
        private Vector2 _mouseDragStart;
        private float _dragSensitivity = 0.005f;

        // Camera
        private Vector2 _cameraPosition;

        // Dashboard controls - pohyblivé
        private List<DashboardControl> _dashboardControls;
        private float _controlRotation;
        private const float CONTROL_ROTATION_SPEED = 1.5f;

        // Barvy pro levý horní roh
        private List<ColorBar> _colorBars;
        private float _colorBarTimer;

        class DashboardControl
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public string Label;
            public float Rotation;
            public Color Color;
            public float Pulse;
            public float Speed;

            public DashboardControl(Vector2 position, Vector2 velocity, string label, Color color, float speed)
            {
                Position = position;
                Velocity = velocity;
                Label = label;
                Rotation = 0;
                Color = color;
                Pulse = 0;
                Speed = speed;
            }
        }

        class ColorBar
        {
            public float Height;
            public float TargetHeight;
            public Color Color;
            public float Pulse;
            public float Width;

            public ColorBar(float height, Color color, float width)
            {
                Height = height;
                TargetHeight = height;
                Color = color;
                Pulse = 0;
                Width = width;
            }
        }

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _random = new Random();

            // Nastavení fullscreen
            _graphics.IsFullScreen = true;
            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            // Získání aktuálního rozlišení pro fullscreen
            int screenWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            int screenHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

            _graphics.PreferredBackBufferWidth = screenWidth;
            _graphics.PreferredBackBufferHeight = screenHeight;
            _graphics.IsFullScreen = true;
            _graphics.ApplyChanges();

            ResetGame();

            base.Initialize();
        }

        private void ResetGame()
        {
            _snake = new List<Vector2> { new Vector2(20, GRID_HEIGHT / 2) };
            _vehicle = new Car(new Vector2(20, GRID_HEIGHT / 2) * CELL_SIZE);
            _gameOver = false;
            _victory = false;
            _timer = 0f;
            _steeringWheel = 0f;
            _foodEaten = 0;
            _cameraPosition = Vector2.Zero;
            _obstacleHashSet = new HashSet<Point>();
            _dashboardControls = new List<DashboardControl>();
            _colorBars = new List<ColorBar>();
            _colorBarTimer = 0f;

            GenerateObstacles();
            GenerateFoods();
            GenerateDashboardControls();
            GenerateColorBars();
        }

        private void GenerateObstacles()
        {
            _obstacles = new List<Vector2>();
            _obstacleHashSet.Clear();

            for (int i = 0; i < 120; i++)
            {
                Vector2 obstacle = new Vector2(
                    _random.Next(40, GRID_WIDTH - 40),
                    _random.Next(10, GRID_HEIGHT - 10)
                );

                float distanceFromStart = Vector2.Distance(obstacle, new Vector2(20, GRID_HEIGHT / 2));
                if (distanceFromStart > 30 && !_obstacleHashSet.Contains(new Point((int)obstacle.X, (int)obstacle.Y)))
                {
                    _obstacles.Add(obstacle);
                    _obstacleHashSet.Add(new Point((int)obstacle.X, (int)obstacle.Y));

                    if (_random.Next(0, 100) < 30)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            Vector2 clusterObstacle = obstacle + new Vector2(
                                _random.Next(-3, 4),
                                _random.Next(-3, 4)
                            );
                            Point clusterPoint = new Point((int)clusterObstacle.X, (int)clusterObstacle.Y);
                            if (clusterObstacle.X >= 0 && clusterObstacle.X < GRID_WIDTH &&
                                clusterObstacle.Y >= 0 && clusterObstacle.Y < GRID_HEIGHT &&
                                !_obstacleHashSet.Contains(clusterPoint))
                            {
                                _obstacles.Add(clusterObstacle);
                                _obstacleHashSet.Add(clusterPoint);
                            }
                        }
                    }
                }
            }
        }

        private void GenerateFoods()
        {
            _foods = new List<Vector2>();

            for (int i = 0; i < MAX_FOOD_COUNT; i++)
            {
                int attempts = 0;
                bool placed = false;

                while (!placed && attempts < 100)
                {
                    attempts++;
                    Vector2 food = new Vector2(
                        _random.Next(0, GRID_WIDTH),
                        _random.Next(0, GRID_HEIGHT)
                    );

                    bool positionFree = true;
                    Point foodPoint = new Point((int)food.X, (int)food.Y);

                    if (_obstacleHashSet.Contains(foodPoint))
                        positionFree = false;

                    foreach (var segment in _snake)
                    {
                        if (Vector2.Distance(food, segment) < 2)
                        {
                            positionFree = false;
                            break;
                        }
                    }

                    foreach (var existingFood in _foods)
                    {
                        if (Vector2.Distance(food, existingFood) < 3)
                        {
                            positionFree = false;
                            break;
                        }
                    }

                    if (positionFree && Vector2.Distance(food, new Vector2(20, GRID_HEIGHT / 2)) > 25)
                    {
                        _foods.Add(food);
                        placed = true;
                    }
                }
            }
        }

        private void GenerateDashboardControls()
        {
            _dashboardControls.Clear();

            // Více kontrol, které létají po celé obrazovce
            string[] labels = { "X", "Y", "Z", "Ω", "Δ", "α", "β", "γ", "δ", "ε" };
            Color[] colors = { Color.Red, Color.Blue, Color.Green, Color.Yellow, Color.Purple,
                               Color.Cyan, Color.Magenta, Color.Orange, Color.Pink, Color.LightGreen };

            int screenWidth = _graphics.PreferredBackBufferWidth;
            int screenHeight = _graphics.PreferredBackBufferHeight;

            for (int i = 0; i < labels.Length; i++)
            {
                Vector2 position = new Vector2(
                    _random.Next(50, screenWidth - 50),
                    _random.Next(50, screenHeight - 50)
                );

                Vector2 velocity = new Vector2(
                    (float)(_random.NextDouble() * 2 - 1) * 50,
                    (float)(_random.NextDouble() * 2 - 1) * 50
                );

                float speed = 30 + (float)_random.NextDouble() * 70;

                _dashboardControls.Add(new DashboardControl(position, velocity, labels[i], colors[i % colors.Length], speed));
            }
        }

        private void GenerateColorBars()
        {
            _colorBars.Clear();

            // 8 barevných čar v levém horním rohu
            Color[] colors = { Color.Red, Color.Orange, Color.Yellow, Color.Green,
                               Color.Cyan, Color.Blue, Color.Purple, Color.Magenta };

            for (int i = 0; i < 8; i++)
            {
                float height = 20 + (float)_random.NextDouble() * 100;
                float width = 15 + i * 5;
                _colorBars.Add(new ColorBar(height, colors[i], width));
            }
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (_gameOver)
            {
                if (Keyboard.GetState().IsKeyDown(Keys.Space))
                    ResetGame();
                return;
            }

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            HandleVehicleControls(gameTime);
            UpdateCamera();
            UpdateDashboardControls(deltaTime);
            UpdateColorBars(deltaTime);

            _timer += deltaTime;

            if (_timer >= MOVE_DELAY)
            {
                _timer = 0f;

                Vector2 newHead = new Vector2(
                    _vehicle.Position.X / CELL_SIZE,
                    _vehicle.Position.Y / CELL_SIZE
                );

                Point gridHead = new Point(
                    (int)(newHead.X + 0.5f),
                    (int)(newHead.Y + 0.5f)
                );

                if (gridHead.X < 0 || gridHead.X >= GRID_WIDTH ||
                    gridHead.Y < 0 || gridHead.Y >= GRID_HEIGHT ||
                    _obstacleHashSet.Contains(gridHead))
                {
                    _gameOver = true;
                    _victory = false;
                    return;
                }

                for (int i = 1; i < _snake.Count; i++)
                {
                    if (Vector2.Distance(newHead, _snake[i]) < 0.8f)
                    {
                        _gameOver = true;
                        _victory = false;
                        return;
                    }
                }

                _snake.Insert(0, newHead);

                bool ateFood = false;
                for (int i = _foods.Count - 1; i >= 0; i--)
                {
                    if (Vector2.Distance(newHead, _foods[i]) < 0.8f)
                    {
                        _foods.RemoveAt(i);
                        _foodEaten++;
                        ateFood = true;

                        _vehicle.IncreaseSpeed(0.1f);

                        if (_foodEaten >= WIN_FOOD_COUNT)
                        {
                            _victory = true;
                            _gameOver = true;
                            return;
                        }
                        break;
                    }
                }

                if (!ateFood && _snake.Count > 1)
                {
                    _snake.RemoveAt(_snake.Count - 1);
                }

                if (_foods.Count < MAX_FOOD_COUNT / 2)
                {
                    AddMoreFood();
                }
            }

            base.Update(gameTime);
        }

        private void UpdateDashboardControls(float deltaTime)
        {
            _controlRotation += CONTROL_ROTATION_SPEED * deltaTime;

            int screenWidth = _graphics.PreferredBackBufferWidth;
            int screenHeight = _graphics.PreferredBackBufferHeight;

            foreach (var control in _dashboardControls)
            {
                // Rotace
                control.Rotation += control.Speed * deltaTime;

                // Pohyb
                control.Position += control.Velocity * deltaTime;

                // Odraz od okrajů obrazovky
                if (control.Position.X < 30 || control.Position.X > screenWidth - 30)
                {
                    control.Velocity.X = -control.Velocity.X;
                    control.Position.X = MathHelper.Clamp(control.Position.X, 30, screenWidth - 30);
                }

                if (control.Position.Y < 30 || control.Position.Y > screenHeight - 30)
                {
                    control.Velocity.Y = -control.Velocity.Y;
                    control.Position.Y = MathHelper.Clamp(control.Position.Y, 30, screenHeight - 30);
                }

                // Pulse efekt
                control.Pulse = (float)Math.Sin(_controlRotation * 3 + control.Position.X * 0.01f) * 0.3f + 0.7f;
            }
        }

        private void UpdateColorBars(float deltaTime)
        {
            _colorBarTimer += deltaTime;

            for (int i = 0; i < _colorBars.Count; i++)
            {
                var bar = _colorBars[i];

                // Měníme cílovou výšku čar podle času
                if (_colorBarTimer > i * 0.5f)
                {
                    bar.TargetHeight = 20 + (float)Math.Sin(_colorBarTimer * 2 + i) * 80 + 50;
                }

                // Plynulá animace výšky
                bar.Height = MathHelper.Lerp(bar.Height, bar.TargetHeight, deltaTime * 5);

                // Pulse efekt pro barvu
                bar.Pulse = (float)Math.Sin(_colorBarTimer * 3 + i) * 0.2f + 0.8f;
            }
        }

        private void AddMoreFood()
        {
            int toAdd = MAX_FOOD_COUNT - _foods.Count;

            for (int i = 0; i < toAdd; i++)
            {
                int attempts = 0;
                bool placed = false;

                while (!placed && attempts < 50)
                {
                    attempts++;
                    Vector2 food = new Vector2(
                        _random.Next(0, GRID_WIDTH),
                        _random.Next(0, GRID_HEIGHT)
                    );

                    bool positionFree = true;
                    Point foodPoint = new Point((int)food.X, (int)food.Y);

                    if (_obstacleHashSet.Contains(foodPoint))
                        positionFree = false;

                    foreach (var segment in _snake)
                    {
                        if (Vector2.Distance(food, segment) < 2)
                        {
                            positionFree = false;
                            break;
                        }
                    }

                    foreach (var existingFood in _foods)
                    {
                        if (Vector2.Distance(food, existingFood) < 3)
                        {
                            positionFree = false;
                            break;
                        }
                    }

                    if (positionFree)
                    {
                        _foods.Add(food);
                        placed = true;
                    }
                }
            }
        }

        private void HandleVehicleControls(GameTime gameTime)
        {
            MouseState mouseState = Mouse.GetState();

            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                if (!_isMouseDragging)
                {
                    _isMouseDragging = true;
                    _mouseDragStart = new Vector2(mouseState.X, mouseState.Y);
                }
                else
                {
                    float dragX = mouseState.X - _mouseDragStart.X;
                    _steeringWheel = MathHelper.Clamp(dragX * _dragSensitivity, -MAX_STEERING, MAX_STEERING);
                }
            }
            else
            {
                _isMouseDragging = false;
                _steeringWheel = MathHelper.Lerp(_steeringWheel, 0, 0.1f);
            }

            for (int i = 0; i < _gearKeys.Length; i++)
            {
                if (Keyboard.GetState().IsKeyDown(_gearKeys[i]))
                {
                    _vehicle.ShiftGear(i);
                }
            }

            float acceleration = Keyboard.GetState().IsKeyDown(Keys.Space) ? 1.0f : 0.0f;

            _vehicle.Turn(_steeringWheel);
            _vehicle.Accelerate(acceleration);
            _vehicle.Update(gameTime);
        }

        private void UpdateCamera()
        {
            Vector2 targetCamera = _vehicle.Position - new Vector2(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2);
            _cameraPosition = Vector2.Lerp(_cameraPosition, targetCamera, 0.05f);

            _cameraPosition.X = MathHelper.Clamp(_cameraPosition.X, 0, GRID_WIDTH * CELL_SIZE - _graphics.PreferredBackBufferWidth);
            _cameraPosition.Y = MathHelper.Clamp(_cameraPosition.Y, 0, GRID_HEIGHT * CELL_SIZE - _graphics.PreferredBackBufferHeight);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(25, 25, 30));

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap);

            Vector2 cameraOffset = -_cameraPosition;

            DrawGrid(cameraOffset);

            foreach (var obstacle in _obstacles)
            {
                DrawRectangle(
                    (int)(obstacle.X * CELL_SIZE + cameraOffset.X),
                    (int)(obstacle.Y * CELL_SIZE + cameraOffset.Y),
                    CELL_SIZE, CELL_SIZE, Color.Yellow);
            }

            for (int i = 0; i < _snake.Count; i++)
            {
                float progress = (float)i / _snake.Count;
                Color segmentColor = Color.Lerp(Color.Lime, Color.DarkGreen, progress * 0.7f);
                float scale = 1.0f - (progress * 0.4f);

                DrawRectangle(
                    (int)(_snake[i].X * CELL_SIZE + cameraOffset.X + (CELL_SIZE * (1 - scale)) / 2),
                    (int)(_snake[i].Y * CELL_SIZE + cameraOffset.Y + (CELL_SIZE * (1 - scale)) / 2),
                    (int)(CELL_SIZE * scale), (int)(CELL_SIZE * scale), segmentColor);
            }

            foreach (var food in _foods)
            {
                DrawApple(
                    (int)(food.X * CELL_SIZE + cameraOffset.X),
                    (int)(food.Y * CELL_SIZE + cameraOffset.Y));
            }

            // Barevné čáry v levém horním rohu
            DrawColorBars();

            DrawUI();
            DrawDashboardControls();

            if (_gameOver)
            {
                DrawGameOverScreen();
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawColorBars()
        {
            int startX = 50;
            int startY = 50;
            int spacing = 5;

            for (int i = 0; i < _colorBars.Count; i++)
            {
                var bar = _colorBars[i];

                // Barva s pulzním efektem
                Color barColor = new Color(
                    (byte)(bar.Color.R * bar.Pulse),
                    (byte)(bar.Color.G * bar.Pulse),
                    (byte)(bar.Color.B * bar.Pulse)
                );

                DrawRectangle(
                    startX + i * ((int)bar.Width + spacing),
                    startY,
                    (int)bar.Width,
                    (int)bar.Height,
                    barColor);
            }
        }

        private void DrawApple(int x, int y)
        {
            int centerX = x + CELL_SIZE / 2;
            int centerY = y + CELL_SIZE / 2;
            int radius = CELL_SIZE / 2 - 2;

            for (int i = 0; i < 360; i += 10)
            {
                float angle = MathHelper.ToRadians(i);
                float cos = (float)Math.Cos(angle);
                float sin = (float)Math.Sin(angle);

                int x1 = (int)(centerX + cos * radius);
                int y1 = (int)(centerY + sin * radius);
                int x2 = (int)(centerX + cos * (radius * 0.7f));
                int y2 = (int)(centerY + sin * (radius * 0.7f));

                Color appleColor = Color.Lerp(Color.Red, Color.DarkRed, (float)i / 360f);
                DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), 2, appleColor);
            }

            DrawLine(new Vector2(centerX, y), new Vector2(centerX, y - 5), 2, Color.Brown);
        }

        private void DrawGrid(Vector2 offset)
        {
            int screenWidth = _graphics.PreferredBackBufferWidth;
            int screenHeight = _graphics.PreferredBackBufferHeight;

            int startX = (int)(-offset.X / CELL_SIZE) - 1;
            int endX = startX + (int)(screenWidth / CELL_SIZE) + 2;
            int startY = (int)(-offset.Y / CELL_SIZE) - 1;
            int endY = startY + (int)(screenHeight / CELL_SIZE) + 2;

            Color gridColor = new Color(40, 40, 45, 80);

            for (int x = startX; x <= endX; x++)
            {
                if (x >= 0 && x < GRID_WIDTH)
                {
                    Vector2 start = new Vector2(x * CELL_SIZE + offset.X, 0);
                    Vector2 end = new Vector2(x * CELL_SIZE + offset.X, screenHeight);
                    DrawLine(start, end, 1, gridColor);
                }
            }

            for (int y = startY; y <= endY; y++)
            {
                if (y >= 0 && y < GRID_HEIGHT)
                {
                    Vector2 start = new Vector2(0, y * CELL_SIZE + offset.Y);
                    Vector2 end = new Vector2(screenWidth, y * CELL_SIZE + offset.Y);
                    DrawLine(start, end, 1, gridColor);
                }
            }
        }

        private void DrawUI()
        {
            int screenWidth = _graphics.PreferredBackBufferWidth;
            int screenHeight = _graphics.PreferredBackBufferHeight;

            // VOLANT (v pravém dolním rohu)
            int wheelCenterX = screenWidth - 300;
            int wheelCenterY = screenHeight - 200;
            int wheelRadius = 100;

            DrawCircle(wheelCenterX, wheelCenterY, wheelRadius, new Color(60, 60, 65), 32);
            DrawCircle(wheelCenterX, wheelCenterY, wheelRadius - 5, new Color(80, 80, 85), 32);
            DrawCircle(wheelCenterX, wheelCenterY, wheelRadius - 25, new Color(40, 40, 45), 32);

            // Označení směrů - ČERVENÁ NAHORĚ (N = sever = nahoru)
            string[] directions = { "N", "E", "S", "W" };
            for (int i = 0; i < 4; i++)
            {
                float angle = MathHelper.ToRadians(i * 90);
                int dirX = (int)(wheelCenterX + Math.Cos(angle) * (wheelRadius - 15));
                int dirY = (int)(wheelCenterY + Math.Sin(angle) * (wheelRadius - 15));

                Color dirColor = i == 0 ? Color.Red : Color.LightGray; // N (nahoře) je červená
                DrawControlLabel(dirX, dirY, directions[i], dirColor);
            }

            float wheelAngle = _steeringWheel / MAX_STEERING * 180;
            Vector2 indicator = new Vector2(0, -wheelRadius + 20);
            indicator = RotateVector(indicator, MathHelper.ToRadians(wheelAngle));

            DrawLine(
                new Vector2(wheelCenterX, wheelCenterY),
                new Vector2(wheelCenterX + indicator.X, wheelCenterY + indicator.Y),
                6, Color.Cyan);

            DrawCircle(wheelCenterX, wheelCenterY, 10, Color.Gold, 12);

            // PŘEVODOVKA (pod volantem)
            int gearX = screenWidth - 600;
            int gearY = screenHeight - 200;
            for (int i = 0; i < 3; i++)
            {
                bool isActive = i == _vehicle.CurrentGear;
                Color gearColor = isActive ? Color.Gold : new Color(100, 100, 110);
                Color borderColor = isActive ? Color.Orange : new Color(70, 70, 75);

                DrawRectangle(gearX + i * 80, gearY, 60, 60, borderColor);
                DrawRectangle(gearX + i * 80 + 3, gearY + 3, 54, 54, gearColor);

                DrawControlLabel(gearX + i * 80 + 30, gearY + 30, (i + 1).ToString(),
                    isActive ? Color.Black : Color.White);
            }

            // RYCHLOMĚR (vedle převodovky)
            int speedX = screenWidth - 800;
            int speedY = screenHeight - 200;
            float speedPercent = MathHelper.Clamp(_vehicle.Speed / (_vehicle.MaxSpeed + 0.1f), 0, 1);

            DrawRectangle(speedX, speedY, 200, 60, new Color(40, 40, 45));

            int speedWidth = (int)(200 * speedPercent);
            if (speedWidth > 0)
            {
                Color speedColor = Color.Lerp(Color.Green, Color.Red, speedPercent);
                DrawRectangle(speedX, speedY, speedWidth, 60, speedColor);
            }

            DrawRectangle(speedX, speedY, 200, 60, new Color(80, 80, 85), false);

            // JABLKA (v pravém horním rohu)
            int foodX = screenWidth - 500;
            int foodY = 100;
            for (int i = 0; i < WIN_FOOD_COUNT; i++)
            {
                float pulse = (float)Math.Sin(_timer * 10 + i * 0.5f) * 0.2f + 0.8f;
                Color appleColor = i < _foodEaten ?
                    Color.Lerp(Color.Red, Color.Orange, pulse) :
                    new Color(60, 0, 0);

                int appleSize = i < _foodEaten ? 12 : 10;
                DrawCircle(foodX + i * 30, foodY, appleSize, appleColor, 8);

                if (i < _foodEaten)
                {
                    DrawLine(
                        new Vector2(foodX + i * 30, foodY - appleSize),
                        new Vector2(foodX + i * 30, foodY - appleSize - 3),
                        2, Color.Brown);
                }
            }

            // INFORMAČNÍ PANEL (v levém horním rohu pod barevnými čarami)
            DrawRectangle(50, 200, 400, 180, new Color(0, 0, 0, 180));

            DrawControlLabel(70, 230, $"Jablka: {_foodEaten}/{WIN_FOOD_COUNT}", Color.White);
            DrawControlLabel(70, 270, $"Rychlost: {_vehicle.Speed:F1}",
                Color.Lerp(Color.Green, Color.Red, speedPercent));
            DrawControlLabel(70, 310, $"Převod: {_vehicle.CurrentGear}",
                _vehicle.CurrentGear > 0 ? Color.Gold : Color.White);

            if (_vehicle is Car car)
            {
                float baseSpeed = car.GearMaxSpeeds[car.CurrentGear];
                float speedBonus = car.MaxSpeed - baseSpeed;
                DrawControlLabel(70, 350, $"Bonus rychlosti: +{speedBonus:F1}", Color.Cyan);
            }
            else
            {
                DrawControlLabel(70, 350, "Bonus rychlosti: +0.0", Color.Cyan);
            }
        }

        private void DrawDashboardControls()
        {
            foreach (var control in _dashboardControls)
            {
                int size = 30 + (int)(control.Pulse * 20);
                int pulseSize = size;

                // Podklad kontrolky
                DrawCircle((int)control.Position.X, (int)control.Position.Y, pulseSize + 8,
                    new Color(30, 30, 35, 150), 16);

                // Rotující kontrolka
                DrawRotatedControl((int)control.Position.X, (int)control.Position.Y,
                    pulseSize, control.Color, control.Rotation, control.Label);
            }
        }

        private void DrawRotatedControl(int x, int y, int size, Color color, float rotation, string label)
        {
            Vector2[] points = new Vector2[4];
            float halfSize = size / 2f;

            points[0] = new Vector2(-halfSize, -halfSize);
            points[1] = new Vector2(halfSize, -halfSize);
            points[2] = new Vector2(halfSize, halfSize);
            points[3] = new Vector2(-halfSize, halfSize);

            float cos = (float)Math.Cos(rotation);
            float sin = (float)Math.Sin(rotation);

            for (int i = 0; i < 4; i++)
            {
                Vector2 rotated = new Vector2(
                    points[i].X * cos - points[i].Y * sin,
                    points[i].X * sin + points[i].Y * cos
                );
                points[i] = new Vector2(x + rotated.X, y + rotated.Y);
            }

            for (int i = 0; i < 4; i++)
            {
                int next = (i + 1) % 4;
                DrawLine(points[i], points[next], 3, color);
            }

            DrawControlLabel(x, y, label, color);
        }

        private void DrawGameOverScreen()
        {
            int screenWidth = _graphics.PreferredBackBufferWidth;
            int screenHeight = _graphics.PreferredBackBufferHeight;

            DrawRectangle(screenWidth / 2 - 400, screenHeight / 2 - 240, 800, 480, new Color(0, 0, 0, 220));

            if (_victory)
            {
                DrawControlLabel(screenWidth / 2, screenHeight / 2 - 100, "VÝHRA!", Color.Gold, true);
                DrawControlLabel(screenWidth / 2, screenHeight / 2, $"Sebral jsi {WIN_FOOD_COUNT} jablek!", Color.Lime, true);
                DrawControlLabel(screenWidth / 2, screenHeight / 2 + 50, $"Maximální rychlost: {_vehicle.MaxSpeed:F1}", Color.Cyan, true);
            }
            else
            {
                DrawControlLabel(screenWidth / 2, screenHeight / 2 - 100, "GAME OVER", Color.Red, true);
                DrawControlLabel(screenWidth / 2, screenHeight / 2, $"Máš {_foodEaten}/{WIN_FOOD_COUNT} jablek", Color.White, true);
            }

            DrawControlLabel(screenWidth / 2, screenHeight / 2 + 150, "Stiskni SPACE pro restart", Color.White, true);
        }

        private void DrawRectangle(int x, int y, int width, int height, Color color, bool fill = true)
        {
            if (fill)
            {
                _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, width, height), color);
            }
            else
            {
                _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, width, 2), color);
                _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y + height - 2, width, 2), color);
                _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, 2, height), color);
                _spriteBatch.Draw(_pixelTexture, new Rectangle(x + width - 2, y, 2, height), color);
            }
        }

        private void DrawCircle(int x, int y, int radius, Color color, int segments = 16)
        {
            Vector2 center = new Vector2(x, y);
            Vector2 lastPoint = center + new Vector2(radius, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * MathHelper.TwoPi / segments;
                Vector2 nextPoint = center + new Vector2(
                    (float)Math.Cos(angle) * radius,
                    (float)Math.Sin(angle) * radius
                );

                DrawLine(lastPoint, nextPoint, radius / 8, color);
                lastPoint = nextPoint;
            }
        }

        private void DrawControlLabel(int x, int y, string text, Color color, bool centered = false)
        {
            if (centered)
            {
                x -= text.Length * 4;
            }

            for (int i = 0; i < text.Length; i++)
            {
                DrawRectangle(x + i * 12, y, 8, 12, color);
            }
        }

        private void DrawLine(Vector2 start, Vector2 end, int thickness, Color color)
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);

            _spriteBatch.Draw(_pixelTexture,
                new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), thickness),
                null,
                color,
                angle,
                new Vector2(0, 0.5f),
                SpriteEffects.None, 0);
        }

        private Vector2 RotateVector(Vector2 vector, float angle)
        {
            return new Vector2(
                vector.X * (float)Math.Cos(angle) - vector.Y * (float)Math.Sin(angle),
                vector.X * (float)Math.Sin(angle) + vector.Y * (float)Math.Cos(angle)
            );
        }
    }
}