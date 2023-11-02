using System;
using System.Runtime.InteropServices;

namespace rt
{
    class RayTracer
    {
        private Geometry[] geometries;
        private Light[] lights;

        public RayTracer(Geometry[] geometries, Light[] lights)
        {
            this.geometries = geometries;
            this.lights = lights;
        }

        private double ImageToViewPlane(int n, int imgSize, double viewPlaneSize)
        {
            return -n * viewPlaneSize / imgSize + viewPlaneSize / 2;
        }

        private Intersection FindFirstIntersection(Line ray, double minDist, double maxDist)
        {
            var intersection = new Intersection();

            foreach (var geometry in geometries)
            {
                var intr = geometry.GetIntersection(ray, minDist, maxDist);

                if (!intr.Valid || !intr.Visible) continue;

                if (!intersection.Valid || !intersection.Visible)
                {
                    intersection = intr;
                }
                else if (intr.T < intersection.T)
                {
                    intersection = intr;
                }
            }

            return intersection;
        }

        private bool IsLit(Vector point, Light light)
        {
			// TODO: ADD CODE HERE
			var line = new Line(light.Position, point);

			var frontPlaneDistance = 0.0;
			var backPlaneDistance = 1000.0;
			var intersection = FindFirstIntersection(line, frontPlaneDistance, backPlaneDistance);
			if (!intersection.Valid || !intersection.Visible)
				return true;
			return intersection.T > (light.Position - point).Length() - 0.001;
		}

        public void Render(Camera camera, int width, int height, string filename)
		{
			var background = new Color();
			var viewParallel = (camera.Up ^ camera.Direction).Normalize();

			var image = new Image(width, height);

			var vecW = camera.Direction * camera.ViewPlaneDistance;
			for (var i = 0; i < width; i++)
			{
				for (var j = 0; j < height; j++)
				{
					var x1 = camera.Position +
						vecW +
						viewParallel * ImageToViewPlane(i, width, camera.ViewPlaneWidth) +
						camera.Up * ImageToViewPlane(j, height, camera.ViewPlaneHeight);
					var ray = new Line(camera.Position, x1);
					var intersection = FindFirstIntersection(ray, camera.FrontPlaneDistance, camera.BackPlaneDistance);
					if (intersection.Valid && intersection.Visible)
					{
						var color = new Color();
						foreach (var light in lights)
						{
							var sumOfColorsFromLight = new Color(intersection.Geometry.Material.Ambient * light.Ambient);
							if (IsLit(intersection.Position, light))
							{
								//v
								var intersectionPositionVector = intersection.Position;
								//e
								var vectorFromTheIntersectionPointToTheCamera = (camera.Position - intersection.Position).Normalize();
								//n
								var normalToTheSurfaceAtTheIntersectionPoint = intersection.Normal;
								//t
								var vectorFromTheIntersectionPointToTheLight = (light.Position - intersection.Position).Normalize();
								//r
								var reflectionVector =
									(normalToTheSurfaceAtTheIntersectionPoint *
									(normalToTheSurfaceAtTheIntersectionPoint * vectorFromTheIntersectionPointToTheLight * 2)
									- vectorFromTheIntersectionPointToTheLight)
									.Normalize();
								var n_times_t = normalToTheSurfaceAtTheIntersectionPoint * vectorFromTheIntersectionPointToTheLight;

								if (n_times_t > 0)
								{
									sumOfColorsFromLight += intersection.Geometry.Material.Diffuse *
										light.Diffuse *
										(n_times_t);
								}

								var e_times_r = vectorFromTheIntersectionPointToTheCamera * reflectionVector;
								if (e_times_r > 0)
								{
									sumOfColorsFromLight += intersection.Geometry.Material.Specular *
										light.Specular *
										Math.Pow(e_times_r, intersection.Geometry.Material.Shininess);
								}
								sumOfColorsFromLight *= light.Intensity;
							}
							color += sumOfColorsFromLight;
						}
						image.SetPixel(i, j, color);
					}
					else
						image.SetPixel(i, j, background);
				}
			}

			image.Store(filename);
		}
	}

}