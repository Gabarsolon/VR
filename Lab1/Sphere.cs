using System;

namespace rt
{
    public class Sphere : Geometry
    {
        private Vector Center { get; set; }
        private double Radius { get; set; }

        public Sphere(Vector center, double radius, Material material, Color color) : base(material, color)
        {
            Center = center;
            Radius = radius;
        }

        public override Intersection GetIntersection(Line line, double minDist, double maxDist)
        {
			//ADD CODE HERE

			//(Dx*t+Xo-Vc)^2 = R^2
			//Dx^2 + 2Dxt(Xo-Vc) + (Xo-Vc)^2-R^2 = 0
			//  a        b                c

			//Dx^2
			var a = line.Dx * line.Dx;
			//2Dxt(Xo-Vc)
			var b = (line.Dx * line.X0 ) * 2;
			b -= (line.Dx * Center) * 2;
			//(Xo-Vc)^2-R^2
			var c = (line.X0 * line.X0) - 2 * (line.X0 * Center)+ (Center * Center) - (Radius * Radius);

			var discriminant = (b * b) - (4.0 * a * c);
			if (discriminant < 0.001)
				return new Intersection(false, false, this, line, 0);
			var t1 = -b - Math.Sqrt(discriminant);
			var t2 = -b + Math.Sqrt(discriminant);
			t1 /= 2.0 * a;
			t2 /= 2.0 * a;
			var validT1 = t1 >= minDist && t1 <= maxDist;
			var validT2 = t2 >= minDist && t2 <= maxDist;
			if (!validT1 && !validT2)
				return new Intersection(false, false, this, line, 0);
			if (validT1 && !validT2)
				return new Intersection(true, true, this, line, t1);
			if (!validT1 && validT2)
				return new Intersection(true, true, this, line, t2);
			var minimum_between_t1_and_t2 = t1;
			if (t2 < minimum_between_t1_and_t2)
				minimum_between_t1_and_t2 = t2;
			return new Intersection(true, true, this, line, minimum_between_t1_and_t2);
        }

        public override Vector Normal(Vector v)
        {
            var n = v - Center;
            n.Normalize();
            return n;
        }
    }
}