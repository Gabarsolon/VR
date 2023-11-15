using System;


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

		private Vector Normal(Vector point)
		{
			return new Vector(
				2 * (point.X - Center.X) / SemiAxesLength.X,
				2 * (point.Y - Center.Y) / SemiAxesLength.Y,
				2 * (point.Z - Center.Z) / SemiAxesLength.Z
			).Normalize();
		}

		public override Intersection GetIntersection(Line line, double minDist, double maxDist)
		{
			var a = line.Dx.X;
			var b = line.X0.X;
			var c = line.Dx.Y;
			var d = line.X0.Y;
			var e = line.Dx.Z;
			var f = line.X0.Z;

			var A_squared = SemiAxesLength.X * SemiAxesLength.X;
			var B_squared = SemiAxesLength.Y * SemiAxesLength.Y;
			var C_squared = SemiAxesLength.Z * SemiAxesLength.Z;

			var ap = a * a / A_squared + c * c / B_squared + e * e / C_squared;
			var bp = 2 * (a * (b - Center.X) / A_squared + c * (d - Center.Y) / B_squared + e * (f - Center.Z) / C_squared);
			var cp = (b - Center.X) * (b - Center.X) / A_squared + (d - Center.Y) * (d - Center.Y) / B_squared + (f - Center.Z) * (f - Center.Z) / C_squared - Radius * Radius;
			var delta = bp * bp - 4 * ap * cp;
			if (delta < 0.0001)
			{
				return new Intersection(false, false, this, line, 0, null);
			}
			var t1 = (-bp - Math.Sqrt(bp * bp - 4 * ap * cp)) / (2 * ap);
			var t2 = (-bp + Math.Sqrt(bp * bp + 4 * ap * cp)) / (2 * ap);

			var t1verif = minDist <= t1 && t1 <= maxDist;
			var t2verif = minDist <= t2 && t2 <= maxDist;

			if (t1verif == false && t2verif == false)
				return new Intersection(false, false, this, line, 0, null);
			if (t1verif == false && t2verif == true)
				return new Intersection(true, true, this, line, t2, Normal(line.Dx * t2 + line.X0));
			if (t2verif == false && t1verif == true)
				return new Intersection(true, true, this, line, t1, Normal(line.Dx * t1 + line.X0));

			if (t1 < t2)
				return new Intersection(true, true, this, line, t1, Normal(line.Dx * t1 + line.X0));
			return new Intersection(true, true, this, line, t2, Normal(line.Dx * t2 + line.X0));
		}
	}
}
