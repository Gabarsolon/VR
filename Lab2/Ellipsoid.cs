﻿using System;


namespace rt
{
    public class Ellipsoid : Geometry
    {
        private Vector Center { get; }
        private Vector SemiAxesLength { get; }
        private double Radius { get; }
        
        
        public Ellipsoid(Vector center, Vector semiAxesLength, double radius, Material material, Color color) : base(material, color)
        {
            Center = center;
            SemiAxesLength = semiAxesLength;
            Radius = radius;
        }

        public Ellipsoid(Vector center, Vector semiAxesLength, double radius, Color color) : base(color)
        {
            Center = center;
            SemiAxesLength = semiAxesLength;
            Radius = radius;
        }

        public Vector Normal(Vector point)
        {
			return new Vector(
			   2 * (point.X - center.X) / (A * A),
			   2 * (point.Y - center.Y) / (B * B),
			   2 * (point.Z - center.Z) / (C * C)
		   ).Normalize();
		}

		public override Intersection GetIntersection(Line line, double minDist, double maxDist)
		{
			// Transform the line to the ellipsoid's local space
			Vector localX0 = line.X0 - Center;
			Vector localDx = line.Dx;

			// Calculate coefficients for the quadratic equation
			double a = (localDx.X * localDx.X) / (SemiAxesLength.X * SemiAxesLength.X) +
					   (localDx.Y * localDx.Y) / (SemiAxesLength.Y * SemiAxesLength.Y) +
					   (localDx.Z * localDx.Z) / (SemiAxesLength.Z * SemiAxesLength.Z);

			double b = 2 * (localDx.X * localX0.X) / (SemiAxesLength.X * SemiAxesLength.X) +
					   2 * (localDx.Y * localX0.Y) / (SemiAxesLength.Y * SemiAxesLength.Y) +
					   2 * (localDx.Z * localX0.Z) / (SemiAxesLength.Z * SemiAxesLength.Z);

			double c = (localX0.X * localX0.X) / (SemiAxesLength.X * SemiAxesLength.X) +
					   (localX0.Y * localX0.Y) / (SemiAxesLength.Y * SemiAxesLength.Y) +
					   (localX0.Z * localX0.Z) / (SemiAxesLength.Z * SemiAxesLength.Z) - Radius*Radius;

			// Solve the quadratic equation
			double discriminant = (b * b) - (4.0 * a * c);

			if (discriminant < 0.0)
			{
				// No intersection
				return new Intersection(false, false, this, line, 0, new Vector());
			}

			double t1 = (-b - Math.Sqrt(discriminant)) / (2.0 * a);
			double t2 = (-b + Math.Sqrt(discriminant)) / (2.0 * a);

			if (t1 > maxDist || t2 < minDist)
			{
				// Intersection points are outside the specified range
				return new Intersection(false, false, this, line, 0, new Vector());
			}

			double t = Math.Max(t1, minDist);

			// Calculate the intersection point in world space
			Vector intersectionPoint = localX0 + localDx * t;

			// Calculate the normal vector at the intersection point
			Vector normal = Normal(intersectionPoint);

			return new Intersection(true, true, this, line, t, normal);
		}

	}
}
