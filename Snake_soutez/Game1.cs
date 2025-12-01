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

        // Game objects
        private List<Vector2> _snake;
        private List<Vector2> _foods; // Změněno na více jablek
        private bool _gameOver;
        private bool _victory;

        // Timing
        private float _timer;
        private const float MOVE_DELAY = 0.15f; // Zvýšeno pro lepší výkon

        // Game constants
        private const int CELL_SIZE = 20;
        private const int GRID_WIDTH = 150;
        private const int GRID_HEIGHT = 54;
        private const int WIN_FOOD_COUNT = 15;
        private const int MAX_FOOD_COUNT = 30; // Maximálně 30 jablek na mapě
        private int _foodEaten = 0;

        // Vehicle controls
        private IVehicle _vehicle;
        private float _steeringWheel;
        private const float MAX_STEERING = 1.5f;
        private Keys[] _gearKeys = { Keys.D1, Keys.D2, Keys.D3 };

        // Obstacles
        private List<Vector2> _obstacles;
        private HashSet<Point> _obstacleHashSet; // Pro rychlejší kontrolu kolizí

        // Mouse steering
        private bool _isMouseDragging = false;
        private Vector2 _mouseDragStart;
        private float _dragSensitivity = 0.01f;

        // Camera
        private Vector2 _cameraPosition;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 1920;
            _graphics.PreferredBackBufferHeight = 1080;
            _graphics.ApplyChanges();

            ResetGame();

            base.Initialize();
        }

        private void ResetGame()
        {
            _snake = new List<Vector2> { new Vector2(10, GRID_HEIGHT / 2) };
            _vehicle = new Car(new Vector2(10, GRID_HEIGHT / 2) * CELL_SIZE);
            _gameOver = false;
            _victory = false;
            _timer = 0f;
            _steeringWheel = 0f;
            _foodEaten = 0;
            _cameraPosition = Vector2.Zero;
            _obstacleHashSet = new HashSet<Point>();
            GenerateObstacles();
            GenerateFoods();
        }

        private void GenerateObstacles()
        {
            _obstacles = new List<Vector2>();
            _obstacleHashSet.Clear();
            Random rand = new Random();

            // Zmenšen počet překážek pro lepší výkon
            for (int i = 0; i < 30; i++)
            {
                Vector2 obstacle = new Vector2(
                    rand.Next(20, GRID_WIDTH - 20),
                    rand.Next(5, GRID_HEIGHT - 5)
                );

                float distanceFromStart = Vector2.Distance(obstacle, new Vector2(10, GRID_HEIGHT / 2));
                if (distanceFromStart > 20 && !_obstacleHashSet.Contains(new Point((int)obstacle.X, (int)obstacle.Y)))
                {
                    _obstacles.Add(obstacle);
                    _obstacleHashSet.Add(new Point((int)obstacle.X, (int)obstacle.Y));
                }
            }
        }

        private void GenerateFoods()
        {
            _foods = new List<Vector2>();
            Random rand = new Random();

            // Generujeme více jablek
            for (int i = 0; i < MAX_FOOD_COUNT; i++)
            {
                int attempts = 0;
                bool placed = false;

                while (!placed && attempts < 100)
                {
                    attempts++;
                    Vector2 food = new Vector2(
                        rand.Next(0, GRID_WIDTH),
                        rand.Next(0, GRID_HEIGHT)
                    );

                    float distanceFromStart = Vector2.Distance(food, new Vector2(10, GRID_HEIGHT / 2));

                    // Kontrola, zda je pozice volná
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

                    if (positionFree && distanceFromStart > 15)
                    {
                        _foods.Add(food);
                        placed = true;
                    }
                }
            }
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Vytvoření jednopixelové textury jednou
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

            HandleVehicleControls(gameTime);
            UpdateCamera();

            _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;

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

                // Rychlejší kontrola kolizí s překážkami
                if (gridHead.X < 0 || gridHead.X >= GRID_WIDTH ||
                    gridHead.Y < 0 || gridHead.Y >= GRID_HEIGHT ||
                    _obstacleHashSet.Contains(gridHead))
                {
                    _gameOver = true;
                    _victory = false;
                    return;
                }

                // Kontrola kolize hlavy s tělem
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

                // Kontrola sbírání jablek
                bool ateFood = false;
                for (int i = _foods.Count - 1; i >= 0; i--)
                {
                    if (Vector2.Distance(newHead, _foods[i]) < 0.8f)
                    {
                        _foods.RemoveAt(i);
                        _foodEaten++;
                        ateFood = true;

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

                // Pokud je málo jablek, přidáme nová
                if (_foods.Count < MAX_FOOD_COUNT / 2)
                {
                    AddMoreFood();
                }
            }

            base.Update(gameTime);
        }

        private void AddMoreFood()
        {
            Random rand = new Random();
            int toAdd = MAX_FOOD_COUNT - _foods.Count;

            for (int i = 0; i < toAdd; i++)
            {
                int attempts = 0;
                bool placed = false;

                while (!placed && attempts < 50)
                {
                    attempts++;
                    Vector2 food = new Vector2(
                        rand.Next(0, GRID_WIDTH),
                        rand.Next(0, GRID_HEIGHT)
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
                _steeringWheel = MathHelper.Lerp(_steeringWheel, 0, 0.15f);
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
            Vector2 targetCamera = _vehicle.Position - new Vector2(1920 / 2, 1080 / 2);
            _cameraPosition = Vector2.Lerp(_cameraPosition, targetCamera, 0.08f);

            _cameraPosition.X = MathHelper.Clamp(_cameraPosition.X, 0, GRID_WIDTH * CELL_SIZE - 1920);
            _cameraPosition.Y = MathHelper.Clamp(_cameraPosition.Y, 0, GRID_HEIGHT * CELL_SIZE - 1080);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DarkGray);

            _spriteBatch.Begin();

            Vector2 cameraOffset = -_cameraPosition;

            // Kreslení překážek
            foreach (var obstacle in _obstacles)
            {
                DrawRectangle(
                    (int)(obstacle.X * CELL_SIZE + cameraOffset.X),
                    (int)(obstacle.Y * CELL_SIZE + cameraOffset.Y),
                    CELL_SIZE, CELL_SIZE, Color.Yellow);
            }

            // Kreslení hada - optimalizované
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

            // Kreslení jablek
            foreach (var food in _foods)
            {
                DrawRectangle(
                    (int)(food.X * CELL_SIZE + cameraOffset.X),
                    (int)(food.Y * CELL_SIZE + cameraOffset.Y),
                    CELL_SIZE, CELL_SIZE, Color.Red);
            }

            // Kreslení UI
            DrawUI();

            // Kreslení konce hry
            if (_gameOver)
            {
                if (_victory)
                {
                    DrawRectangle(560, 300, 800, 480, new Color(0, 255, 0, 200));
                }
                else
                {
                    DrawTriangle(760, 400, 1160, 400, 960, 700, Color.Red);
                }
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawUI()
        {
            // Volant
            int wheelX = 100;
            int wheelY = _graphics.PreferredBackBufferHeight - 150;
            int wheelSize = 120;

            DrawRectangle(wheelX, wheelY, wheelSize, wheelSize, Color.DarkGray);

            float wheelAngle = _steeringWheel / MAX_STEERING * 45;
            Vector2 center = new Vector2(wheelX + wheelSize / 2, wheelY + wheelSize / 2);
            Vector2 indicator = new Vector2(0, -wheelSize / 3);

            float cos = (float)Math.Cos(MathHelper.ToRadians(wheelAngle));
            float sin = (float)Math.Sin(MathHelper.ToRadians(wheelAngle));
            Vector2 rotatedIndicator = new Vector2(
                indicator.X * cos - indicator.Y * sin,
                indicator.X * sin + indicator.Y * cos
            );

            DrawLine(center, center + rotatedIndicator, 8, Color.White);

            // Převodovka
            int gearX = 300;
            int gearY = _graphics.PreferredBackBufferHeight - 100;
            for (int i = 0; i < 3; i++)
            {
                Color color = i == _vehicle.CurrentGear ? Color.Gold : Color.White;
                DrawRectangle(gearX + i * 50, gearY, 40, 60, color);
            }

            // Rychloměr
            int speedX = 500;
            int speedY = _graphics.PreferredBackBufferHeight - 100;
            int speedWidth = (int)(MathHelper.Clamp(_vehicle.Speed / 2.4f, 0, 1) * 100);
            DrawRectangle(speedX, speedY, speedWidth, 60, Color.Blue);

            // Počet snědených jablek
            int foodX = 700;
            int foodY = _graphics.PreferredBackBufferHeight - 80;
            for (int i = 0; i < WIN_FOOD_COUNT; i++)
            {
                Color appleColor = i < _foodEaten ? Color.Red : new Color(80, 0, 0);
                DrawRectangle(foodX + i * 25, foodY, 20, 20, appleColor);
            }

            // Informace
            DrawRectangle(100, 50, 250, 80, new Color(0, 0, 0, 128));
        }

        private void DrawRectangle(int x, int y, int width, int height, Color color)
        {
            // Použití předem vytvořené textury
            _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, width, height), color);
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

        private void DrawTriangle(int x1, int y1, int x2, int y2, int x3, int y3, Color color)
        {
            // Jednodušší trojúhelník - pouze obrys
            DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), 3, color);
            DrawLine(new Vector2(x2, y2), new Vector2(x3, y3), 3, color);
            DrawLine(new Vector2(x3, y3), new Vector2(x1, y1), 3, color);
        }
    }
}