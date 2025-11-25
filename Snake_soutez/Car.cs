using Microsoft.Xna.Framework;
using Snake_soutez;
using System;

namespace Snake_soutez
{
    public class Car : IVehicle
    {
        public Vector2 Position { get; private set; }
        public float Speed { get; private set; }
        public int CurrentGear { get; private set; }
        public float Rotation { get; private set; }

        private readonly float[] _gearMaxSpeeds = { 0f, 0.3f, 0.6f, 0.9f }; // ZPOMALENO
        private readonly float _acceleration = 0.08f; // ZPOMALENO
        private readonly float _deceleration = 0.05f;
        private readonly float _steeringSpeed = 0.02f; // ZPOMALENO

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
            Rotation += direction * _steeringSpeed * (Speed + 0.1f);
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
            Speed = MathHelper.Lerp(Speed, 0, _deceleration);

            Vector2 direction = new Vector2((float)Math.Cos(Rotation), (float)Math.Sin(Rotation));
            Position += direction * Speed;
        }
    }
}