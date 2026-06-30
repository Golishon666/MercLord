using System;
using MercLord.Global.Cells;

namespace MercLord.Global.Generation
{
    internal static class SphericalWorldGeometry
    {
        public static float Dot(WorldSpherePoint left, WorldSpherePoint right)
        {
            return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
        }

        public static WorldSpherePoint Normalize(WorldSpherePoint point)
        {
            var magnitude = Math.Sqrt(point.X * point.X + point.Y * point.Y + point.Z * point.Z);
            if (magnitude < 0.0001)
            {
                return new WorldSpherePoint(1f, 0f, 0f);
            }

            return new WorldSpherePoint(
                (float)(point.X / magnitude),
                (float)(point.Y / magnitude),
                (float)(point.Z / magnitude));
        }

        public static WorldSpherePoint RotateAroundAxis(WorldSpherePoint point, WorldSpherePoint axis, double angle)
        {
            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);
            var dot = Dot(point, axis);
            var crossX = axis.Y * point.Z - axis.Z * point.Y;
            var crossY = axis.Z * point.X - axis.X * point.Z;
            var crossZ = axis.X * point.Y - axis.Y * point.X;

            return Normalize(new WorldSpherePoint(
                (float)(point.X * cos + crossX * sin + axis.X * dot * (1.0 - cos)),
                (float)(point.Y * cos + crossY * sin + axis.Y * dot * (1.0 - cos)),
                (float)(point.Z * cos + crossZ * sin + axis.Z * dot * (1.0 - cos))));
        }
    }
}
