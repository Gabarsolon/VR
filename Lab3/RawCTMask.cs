using System;
using System.IO;
using System.Text.RegularExpressions;

namespace rt;

public class RawCtMask: Geometry
{
    private readonly Vector _position;
    private readonly double _scale;
    private readonly ColorMap _colorMap;
    private readonly byte[] _data;

    private readonly int[] _resolution = new int[3];
    private readonly double[] _thickness = new double[3];
    private readonly Vector _v0;
    private readonly Vector _v1;

    public RawCtMask(string datFile, string rawFile, Vector position, double scale, ColorMap colorMap) : base(Color.NONE)
    {
        _position = position;
        _scale = scale;
        _colorMap = colorMap;

        var lines = File.ReadLines(datFile);
        foreach (var line in lines)
        {
            var kv = Regex.Replace(line, "[:\\t ]+", ":").Split(":");
            if (kv[0] == "Resolution")
            {
                _resolution[0] = Convert.ToInt32(kv[1]);
                _resolution[1] = Convert.ToInt32(kv[2]);
                _resolution[2] = Convert.ToInt32(kv[3]);
            } else if (kv[0] == "SliceThickness")
            {
                _thickness[0] = Convert.ToDouble(kv[1]);
                _thickness[1] = Convert.ToDouble(kv[2]);
                _thickness[2] = Convert.ToDouble(kv[3]);
            }
        }

        _v0 = position;
        _v1 = position + new Vector(_resolution[0]*_thickness[0]*scale, _resolution[1]*_thickness[1]*scale, _resolution[2]*_thickness[2]*scale);

        var len = _resolution[0] * _resolution[1] * _resolution[2];
        _data = new byte[len];
        using FileStream f = new FileStream(rawFile, FileMode.Open, FileAccess.Read);
        if (f.Read(_data, 0, len) != len)
        {
            throw new InvalidDataException($"Failed to read the {len}-byte raw data");
        }
    }
    
    private ushort Value(int x, int y, int z)
    {
        if (x < 0 || y < 0 || z < 0 || x >= _resolution[0] || y >= _resolution[1] || z >= _resolution[2])
        {
            return 0;
        }

        return _data[z * _resolution[1] * _resolution[0] + y * _resolution[0] + x];
    }
	public Color sampleColorThroughCube(Line line, double t, int[] oldIndices)
	{
		Vector coordinatePosition = line.CoordinateToPosition(t);
		int[] indexes = GetIndexes(coordinatePosition);

		// TODO: I don't understand why this is correct. The index value is correct in both 0 and in _resolution[0/1/2]. Omit either one and you get weird rendering errors. Not sure why.
		if (indexes[0] < 0 || indexes[1] < 0 || indexes[2] < 0 || indexes[0] > _resolution[0] ||
			indexes[1] > _resolution[1] || indexes[2] > _resolution[2])
		{
			return new Color();
		}

		//Maybe not necessary. This ensures a cell's color is only sampled once.
		if (oldIndices.Length != 0)
		{
			if (oldIndices[0] == indexes[0] && oldIndices[1] == indexes[1] && oldIndices[2] == indexes[2])
				return sampleColorThroughCube(line, t + 1, indexes);
		}

		Color c = GetColor(coordinatePosition);
		return c * c.Alpha + sampleColorThroughCube(line, t + 1, indexes) * (1 - c.Alpha);
	}
	public override Intersection GetIntersection(Line line, double minDist, double maxDist)
    {
        // ADD CODE HERE
        Vector rayDir = line.Dx;
        Vector rayOrig = line.X0;

        double tNear = minDist;
        double tFar = maxDist;

        // Compute intersections with the slab
        for (int i = 0; i < 3; i++)
        {
            double invRayDir, t0, t1;
            if (i == 0)
            {
                invRayDir = 1.0 / rayDir.X;
                t0 = (_v0.X - rayOrig.X) * invRayDir;
                t1 = (_v1.X - rayOrig.X) * invRayDir;
            }
            else if (i == 1)
            {
                invRayDir = 1.0 / rayDir.Y;
                t0 = (_v0.Y - rayOrig.Y) * invRayDir;
                t1 = (_v1.Y - rayOrig.Y) * invRayDir;
            }
            else
            {
                invRayDir = 1.0 / rayDir.Z;
                t0 = (_v0.Z - rayOrig.Z) * invRayDir;
                t1 = (_v1.Z - rayOrig.Z) * invRayDir;
            }

            if (invRayDir < 0.0)
            {
                double temp = t0;
                t0 = t1;
                t1 = temp;
            }

            tNear = t0 > tNear ? t0 : tNear;
            tFar = t1 < tFar ? t1 : tFar;

            if (tNear > tFar)
                return Intersection.NONE;
        }

        // Compute the starting and ending points of the intersection in world coordinates
        Vector pNear = rayOrig + rayDir * tNear;
        Vector pFar = rayOrig + rayDir * tFar;

        // Get the indexes of the starting and ending points
        int[] nearIndexes = GetIndexes(pNear);
        int[] farIndexes = GetIndexes(pFar);

        // Calculate the step size and initialize the current position
        Vector step = (pFar - pNear) / Math.Max(Math.Max(_resolution[0], _resolution[1]), _resolution[2]);
        Vector currentPos = pNear;

        // Traverse the volume along the ray
        while (true)
        {
            int[] indexes = GetIndexes(currentPos);

            if (indexes[0] >= 0 && indexes[0] < _resolution[0] &&
                indexes[1] >= 0 && indexes[1] < _resolution[1] &&
                indexes[2] >= 0 && indexes[2] < _resolution[2])
            {
                ushort value = Value(indexes[0], indexes[1], indexes[2]);
                if (value > 0)
                {
                    // You found an intersection, do something with it.
                    // For now, let's return a basic Intersection object.
                    //return new Intersection(true, tNear, GetColor(currentPos), GetNormal(currentPos), );
                    Color color = sampleColorThroughCube(line, tNear, GetIndexes(line.CoordinateToPosition(tNear)));
                    
					return new Intersection(true, true, this, line, tNear, GetNormal(currentPos), Material.FromColor(color) ,color);
                }
            }

            // Move to the next position
            currentPos += step;

            // Check if you have reached or passed the far intersection point
            if ((currentPos - pFar) * (rayDir) > 0)
                break;
        }
        return Intersection.NONE;
    }
    
    private int[] GetIndexes(Vector v)
    {
        return new []{
            (int)Math.Floor((v.X - _position.X) / _thickness[0] / _scale), 
            (int)Math.Floor((v.Y - _position.Y) / _thickness[1] / _scale),
            (int)Math.Floor((v.Z - _position.Z) / _thickness[2] / _scale)};
    }
    private Color GetColor(Vector v)
    {
        int[] idx = GetIndexes(v);

        ushort value = Value(idx[0], idx[1], idx[2]);
        return _colorMap.GetColor(value);
    }

    private Vector GetNormal(Vector v)
    {
        int[] idx = GetIndexes(v);
        double x0 = Value(idx[0] - 1, idx[1], idx[2]);
        double x1 = Value(idx[0] + 1, idx[1], idx[2]);
        double y0 = Value(idx[0], idx[1] - 1, idx[2]);
        double y1 = Value(idx[0], idx[1] + 1, idx[2]);
        double z0 = Value(idx[0], idx[1], idx[2] - 1);
        double z1 = Value(idx[0], idx[1], idx[2] + 1);

        return new Vector(x1 - x0, y1 - y0, z1 - z0).Normalize();
    }
}