using Microsoft.Xna.Framework;

namespace Snake_soutez
{
    public class Car : IVehicle
    {
        public Vector2 Position { get; private set; }
        public float Speed { get; private set; }
        public int CurrentGear { get; private set; }
        public float Rotation { get; private set; }
        public float MaxSpeed { get; private set; }

        public float[] GearMaxSpeeds { get; private set; }

        private readonly float _baseAcceleration = 0.1f;
        private readonly float _deceleration = 0.08f;
        private readonly float _steeringSpeed = 0.025f;
        private float _speedBonus;

        public Car(Vector2 startPosition)
        {
            Position = startPosition;
            CurrentGear = 1;
            Rotation = 0f;
            GearMaxSpeeds = new float[] { 0f, 1.0f, 2.0f, 3.0f };
            MaxSpeed = GearMaxSpeeds[CurrentGear];
            _speedBonus = 0f;
        }

        public void Accelerate(float power)
        {
            float targetSpeed = (MaxSpeed + _speedBonus) * power;
            Speed = MathHelper.Lerp(Speed, targetSpeed, _baseAcceleration);
        }

        public void Turn(float direction)
        {
            Rotation += direction * _steeringSpeed * MathHelper.Clamp(Speed * 0.3f, 0.1f, 1f);
        }

        public void ShiftGear(int gear)
        {
            if (gear >= 0 && gear < GearMaxSpeeds.Length)
            {
                CurrentGear = gear;
                MaxSpeed = GearMaxSpeeds[CurrentGear] + _speedBonus;
            }
        }

        public void IncreaseSpeed(float amount)
        {
            _speedBonus += amount;
            MaxSpeed = GearMaxSpeeds[CurrentGear] + _speedBonus;
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