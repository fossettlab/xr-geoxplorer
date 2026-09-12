using System;
using System.Linq;
using Newtonsoft.Json;

namespace GeoX.Spatial
{
    public readonly struct Vector3d
    {
        [JsonProperty(Required = Required.Always)] public double X { get; }
        [JsonProperty(Required = Required.Always)] public double Y { get; }
        [JsonProperty(Required = Required.Always)] public double Z { get; }

        [JsonConstructor]
        public Vector3d(double x, double y, double z)
        {
            CheckFinite(x); CheckFinite(y); CheckFinite(z);
            X = x; Y = y; Z = z;
        }

        public static Vector3d operator +(Vector3d a, Vector3d b) =>
            new Vector3d(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3d operator -(Vector3d a, Vector3d b) =>
            new Vector3d(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3d operator *(Vector3d a, double s) =>
            new Vector3d(a.X * s, a.Y * s, a.Z * s);

        internal static void CheckFinite(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentException("non_finite_coordinate");
        }
    }

    /// <summary>Row-major affine map; never decomposes shear into a Unity TRS.</summary>
    public sealed class AffineMap
    {
        private readonly double[] linear;
        [JsonProperty(Required = Required.Always)]
        public double[] Linear => (double[])linear.Clone();
        [JsonProperty(Required = Required.Always)]
        public Vector3d Translation { get; }

        // Binary64 unit roundoff bound with margin for the small matrix solve.
        // This guards numerical usability, not scientific registration accuracy.
        internal const double NumericalTolerance = 64 * 2.2204460492503131e-16;

        [JsonConstructor]
        public AffineMap(double[] linear, Vector3d translation)
        {
            if (linear == null || linear.Length != 9)
                throw new ArgumentException("affine_requires_nine_coefficients");
            foreach (double value in linear) Vector3d.CheckFinite(value);
            this.linear = (double[])linear.Clone();
            Translation = translation;
            InverseLinear();
        }

        public static AffineMap Identity => new AffineMap(
            new double[] { 1, 0, 0, 0, 1, 0, 0, 0, 1 }, default);

        public Vector3d Apply(Vector3d point) => ApplyLinear(point) + Translation;

        public Vector3d ApplyLinear(Vector3d p) => new Vector3d(
            linear[0] * p.X + linear[1] * p.Y + linear[2] * p.Z,
            linear[3] * p.X + linear[4] * p.Y + linear[5] * p.Z,
            linear[6] * p.X + linear[7] * p.Y + linear[8] * p.Z);

        public AffineMap Inverse()
        {
            var inverse = new AffineMap(InverseLinear(), default);
            return new AffineMap(inverse.linear, inverse.ApplyLinear(Translation) * -1);
        }

        public bool IsOrthogonal(bool allowReflection)
        {
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    double dot = 0;
                    for (int k = 0; k < 3; k++) dot += linear[k * 3 + i] * linear[k * 3 + j];
                    if (Math.Abs(dot - (i == j ? 1 : 0)) > NumericalTolerance) return false;
                }
            return allowReflection || Determinant(linear) > 0;
        }

        private double[] InverseLinear()
        {
            double scale = linear.Max(v => Math.Abs(v));
            if (scale == 0) throw new ArgumentException("singular_registration");
            double[] a = linear.Select(v => v / scale).ToArray();
            double determinant = Determinant(a);
            if (determinant == 0) throw new ArgumentException("singular_registration");
            double[] inverse = {
                a[4]*a[8]-a[5]*a[7], a[2]*a[7]-a[1]*a[8], a[1]*a[5]-a[2]*a[4],
                a[5]*a[6]-a[3]*a[8], a[0]*a[8]-a[2]*a[6], a[2]*a[3]-a[0]*a[5],
                a[3]*a[7]-a[4]*a[6], a[1]*a[6]-a[0]*a[7], a[0]*a[4]-a[1]*a[3]
            };
            for (int i = 0; i < inverse.Length; i++) inverse[i] /= determinant;
            double condition = InfinityNorm(a) * InfinityNorm(inverse);
            if (double.IsNaN(condition) || double.IsInfinity(condition) ||
                condition * NumericalTolerance >= 1)
                throw new ArgumentException("ill_conditioned_registration");
            for (int i = 0; i < inverse.Length; i++)
            {
                inverse[i] /= scale;
                Vector3d.CheckFinite(inverse[i]);
            }
            return inverse;
        }

        private static double InfinityNorm(double[] a) => Math.Max(
            Math.Abs(a[0]) + Math.Abs(a[1]) + Math.Abs(a[2]), Math.Max(
            Math.Abs(a[3]) + Math.Abs(a[4]) + Math.Abs(a[5]),
            Math.Abs(a[6]) + Math.Abs(a[7]) + Math.Abs(a[8])));

        private static double Determinant(double[] a) =>
            a[0] * (a[4] * a[8] - a[5] * a[7]) -
            a[1] * (a[3] * a[8] - a[5] * a[6]) +
            a[2] * (a[3] * a[7] - a[4] * a[6]);
    }
}
