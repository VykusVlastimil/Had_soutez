using Microsoft.Xna.Framework;

namespace Snake_soutez
{
    public interface IVehicle
    {
        void Accelerate(float power);
        void Turn(float direction);
        void ShiftGear(int gear);
        void Update(GameTime gameTime);
        Vector2 Position { get; }
        float Speed { get; }
        int CurrentGear { get; }
        float Rotation { get; }
    }
}