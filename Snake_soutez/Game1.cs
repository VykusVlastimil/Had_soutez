using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Snake_soutez;
using System;
using System.Collections.Generic;

namespace Snake_soutez
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // Game objects
        private List<Vector2> _snake;
        private Vector2 _food;
        private bool _gameOver;
        private bool _victory;

        // Timing
        private float _timer;
        private const float MOVE_DELAY = 0.1f;

        // Game constants
        private const int CELL_SIZE = 20;
        private const int GRID_WIDTH = 2000; // ŠIRŠÍ MAPA NA ŠÍŘKU
        private const int GRID_HEIGHT = 54; // 1080 / 20 = 54
        private const int WIN_FOOD_COUNT = 15; // Výhra po 15 jablkách
        private int _foodEaten = 0;

        // Vehicle controls
        private IVehicle _vehicle;
        private float _steeringWheel;
        private const float MAX_STEERING = 1.5f;
        private Keys[] _gearKeys = { Keys.D1, Keys.D2, Keys.D3 };

        // Obstacles
        private List<Vector2> _obstacles;

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
            _vehicle = new Car(new Vector2(10, GRID_HEIGHT / 2));
            _gameOver = false;
            _victory = false;
            _timer = 0f;
            _steeringWheel = 0f;
            _foodEaten = 0;
            _cameraPosition = Vector2.Zero;
            GenerateObstacles();
            SpawnFood();
        }

        private void GenerateObstacles()
        {
            _obstacles = new List<Vector2>();
            Random rand = new Random();

            // Vytvoříme několik překážek v různých částech mapy
            for (int i = 0; i < 50; i++)
            {
                Vector2 obstacle = new Vector2(
                    rand.Next(20, GRID_WIDTH - 20),
                    rand.Next(5, GRID_HEIGHT - 5)
                );

                // Zajistíme, aby překážka nebyla na startovní pozici
                float distanceFromStart = Vector2.Distance(obstacle, new Vector2(10, GRID_HEIGHT / 2));
                if (distanceFromStart > 15 && !_obstacles.Contains(obstacle))
                {
                    _obstacles.Add(obstacle);
                }
            }
        }

        private void SpawnFood()
        {
            var possiblePositions = new List<Vector2>();

            for (int x = 0; x < GRID_WIDTH; x++)
            {
                for (int y = 0; y < GRID_HEIGHT; y++)
                {
                    var pos = new Vector2(x, y);
                    if (!_snake.Contains(pos) && !_obstacles.Contains(pos))
                    {
                        possiblePositions.Add(pos);
                    }
                }
            }

            if (possiblePositions.Count > 0)
            {
                _food = possiblePositions[Random.Shared.Next(possiblePositions.Count)];
            }
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
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
                    (int)(_vehicle.Position.X + 0.5f),
                    (int)(_vehicle.Position.Y + 0.5f)
                );

                // Kontrola kolizí - POUZE HLAVA
                if (newHead.X < 0 || newHead.X >= GRID_WIDTH ||
                    newHead.Y < 0 || newHead.Y >= GRID_HEIGHT ||
                    _obstacles.Contains(newHead))
                {
                    _gameOver = true;
                    _victory = false;
                    return;
                }

                // Kontrola kolize hlavy s tělem (kromě prvního segmentu za hlavou)
                for (int i = 1; i < _snake.Count; i++)
                {
                    if (_snake[i] == newHead)
                    {
                        _gameOver = true;
                        _victory = false;
                        return;
                    }
                }

                _snake.Insert(0, newHead);

                // Kontrola jablka
                if (newHead == _food)
                {
                    _foodEaten++;
                    if (_foodEaten >= WIN_FOOD_COUNT)
                    {
                        _victory = true;
                        _gameOver = true;
                    }
                    else
                    {
                        SpawnFood();
                    }
                }
                else
                {
                    // Odstraníme ocas pouze když nesbíráme jablko
                    if (_snake.Count > 1)
                    {
                        _snake.RemoveAt(_snake.Count - 1);
                    }
                }
            }

            base.Update(gameTime);
        }

        private void HandleVehicleControls(GameTime gameTime)
        {
            MouseState mouseState = Mouse.GetState();

            // Kliknutí a tažení myší pro volant
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
                _steeringWheel = MathHelper.Lerp(_steeringWheel, 0, 0.2f);
            }

            // Řazení (1-3)
            for (int i = 0; i < _gearKeys.Length; i++)
            {
                if (Keyboard.GetState().IsKeyDown(_gearKeys[i]))
                {
                    _vehicle.ShiftGear(i);
                }
            }

            // Plyn (Mezerník)
            float acceleration = Keyboard.GetState().IsKeyDown(Keys.Space) ? 1.0f : 0.0f;

            _vehicle.Turn(_steeringWheel);
            _vehicle.Accelerate(acceleration);
            _vehicle.Update(gameTime);
        }

        private void UpdateCamera()
        {
            // Kamera sleduje auto s mírným předstihem
            Vector2 targetCamera = _vehicle.Position * CELL_SIZE - new Vector2(1920 / 2, 1080 / 2);
            _cameraPosition = Vector2.Lerp(_cameraPosition, targetCamera, 0.1f);

            // Omezení kamery na hranice mapy
            _cameraPosition.X = MathHelper.Clamp(_cameraPosition.X, 0, GRID_WIDTH * CELL_SIZE - 1920);
            _cameraPosition.Y = MathHelper.Clamp(_cameraPosition.Y, 0, GRID_HEIGHT * CELL_SIZE - 1080);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DarkGray);

            _spriteBatch.Begin();

            // Vypočítáme pozici pro kreslení s kamerou
            Vector2 cameraOffset = -_cameraPosition;

            // Kreslení překážek (ŽLUTÉ)
            foreach (var obstacle in _obstacles)
            {
                DrawRectangle(
                    (int)(obstacle.X * CELL_SIZE + cameraOffset.X),
                    (int)(obstacle.Y * CELL_SIZE + cameraOffset.Y),
                    CELL_SIZE, CELL_SIZE, Color.Yellow);
            }

            // Kreslení hada (tělo se táhne za hlavou)
            for (int i = 0; i < _snake.Count; i++)
            {
                Color segmentColor = i == 0 ? Color.Lime : Color.Green; // Hlava světlejší
                float scale = 1.0f - (i * 0.02f); // Tělo se postupně zmenšuje
                scale = Math.Max(scale, 0.6f);

                DrawRectangle(
                    (int)(_snake[i].X * CELL_SIZE + cameraOffset.X + (CELL_SIZE * (1 - scale)) / 2),
                    (int)(_snake[i].Y * CELL_SIZE + cameraOffset.Y + (CELL_SIZE * (1 - scale)) / 2),
                    (int)(CELL_SIZE * scale), (int)(CELL_SIZE * scale), segmentColor);
            }

            // Kreslení jablka
            if (!_gameOver)
            {
                DrawRectangle(
                    (int)(_food.X * CELL_SIZE + cameraOffset.X),
                    (int)(_food.Y * CELL_SIZE + cameraOffset.Y),
                    CELL_SIZE, CELL_SIZE, Color.Red);
            }

            // Kreslení UI (stále na pevné pozici)
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

            // Podstava volantu
            DrawRectangle(wheelX, wheelY, wheelSize, wheelSize, Color.DarkGray);

            // Ukazatel volantu
            float wheelAngle = _steeringWheel / MAX_STEERING * 45;
            Vector2 center = new Vector2(wheelX + wheelSize / 2, wheelY + wheelSize / 2);
            Vector2 indicator = new Vector2(0, -wheelSize / 3);
            indicator = RotateVector(indicator, MathHelper.ToRadians(wheelAngle));

            DrawLine(center, center + indicator, 8, Color.White);

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
            DrawRectangle(speedX, speedY, (int)(_vehicle.Speed * 100), 60, Color.Blue);

            // Počet snědených jablek
            int foodX = 700;
            int foodY = _graphics.PreferredBackBufferHeight - 80;
            for (int i = 0; i < WIN_FOOD_COUNT; i++)
            {
                Color appleColor = i < _foodEaten ? Color.Red : Color.DarkRed;
                DrawRectangle(foodX + i * 25, foodY, 20, 20, appleColor);
            }

            // Textové informace (bez fontu, použijeme obdélníky)
            DrawRectangle(100, 50, 200, 40, new Color(0, 0, 0, 128));
            DrawRectangle(100, 100, 150, 30, new Color(0, 0, 0, 128));
        }

        private void DrawRectangle(int x, int y, int width, int height, Color color)
        {
            var texture = new Texture2D(GraphicsDevice, 1, 1);
            texture.SetData(new[] { color });
            _spriteBatch.Draw(texture, new Rectangle(x, y, width, height), color);
        }

        private void DrawLine(Vector2 start, Vector2 end, int thickness, Color color)
        {
            var texture = new Texture2D(GraphicsDevice, 1, 1);
            texture.SetData(new[] { color });

            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);

            _spriteBatch.Draw(texture,
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

        private void DrawTriangle(int x1, int y1, int x2, int y2, int x3, int y3, Color color)
        {
            var texture = new Texture2D(GraphicsDevice, 1, 1);
            texture.SetData(new[] { color });

            // Jednodušší trojúhelník - vykreslíme 3 čáry
            DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), 3, color);
            DrawLine(new Vector2(x2, y2), new Vector2(x3, y3), 3, color);
            DrawLine(new Vector2(x3, y3), new Vector2(x1, y1), 3, color);
        }
    }
}