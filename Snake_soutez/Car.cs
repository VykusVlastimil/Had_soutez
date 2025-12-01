using Microsoft.Xna.Framework;

namespace Snake_soutez
{
    public class Car : IVehicle
    {
        public Vector2 Position { get; private set; }
        public float Speed { get; private set; }
        public int CurrentGear { get; private set; }
        public float Rotation { get; private set; }

        private readonly float[] _gearMaxSpeeds = { 0f, 0.8f, 1.6f, 2.4f };
        private readonly float _acceleration = 0.1f;
        private readonly float _deceleration = 0.07f;
        private readonly float _steeringSpeed = 0.03f;

        public Car(Vector2 startPosition)
        {
            Position = startPosition;
            CurrentGear = 1;
            Rotation = 0f;
        }

        public void Accelerate(float power)
        {
            float targetSpeed = _gearMaxSpeeds[CurrentGear] * power;
            Speed = MathHelper.Lerp(Speed, targetSpeed, _acceleration);
        }

        public void Turn(float direction)
        {
            Rotation += direction * _steeringSpeed * MathHelper.Clamp(Speed * 0.3f, 0.1f, 1f);
        }

        public void ShiftGear(int gear)
        {
            if (gear >= 0 && gear < _gearMaxSpeeds.Length)
            {
                CurrentGear = gear;
            }
        }

        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Speed = MathHelper.Lerp(Speed, 0, _deceleration * deltaTime * 60f);

            Vector2 direction = new Vector2((float)System.Math.Cos(Rotation), (float)System.Math.Sin(Rotation));
            Position += direction * Speed * deltaTime * 60f;
        }
    }
}