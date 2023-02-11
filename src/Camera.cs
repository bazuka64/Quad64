using OpenTK.Mathematics;
using System;

namespace Quad64.src
{
    public class Camera
    {
        public Vector3 position = new Vector3(10000, 10000, 10000);
        public Vector3 front = -Vector3.UnitZ;
        public Vector3 up = Vector3.UnitY;
        public Vector3 right = Vector3.UnitX;

        float pitch;
        float yaw = -MathHelper.PiOver2;

        float fov = MathHelper.PiOver4;
        public float aspect = 4 / 3f;
        float near = 100;
        float far = 100000;

        public Camera()
        {
            Pitch -= 45;
            Yaw -= 45;
        }

        public float Pitch
        {
            get => MathHelper.RadiansToDegrees(pitch);
            set
            {
                var angle = MathHelper.Clamp(value, -89, 89);
                pitch = MathHelper.DegreesToRadians(angle);
                UpdateVectors();
            }
        }

        public float Yaw
        {
            get => MathHelper.RadiansToDegrees(yaw);
            set
            {
                yaw = MathHelper.DegreesToRadians(value);
                UpdateVectors();
            }
        }

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(position, position + front, up);
        }

        public Matrix4 GetProjectionMatrix()
        {
            return Matrix4.CreatePerspectiveFieldOfView(fov, aspect, near, far);
        }

        void UpdateVectors()
        {
            front.X = MathF.Cos(pitch) * MathF.Cos(yaw);
            front.Y = MathF.Sin(pitch);
            front.Z = MathF.Cos(pitch) * MathF.Sin(yaw);

            front.Normalize();
            right = Vector3.Cross(front, Vector3.UnitY).Normalized();
            up = Vector3.Cross(right, front).Normalized();
        }
    }
}