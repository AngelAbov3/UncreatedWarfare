﻿using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using UnityEngine;

namespace Uncreated.Warfare
{
    public abstract class Zone
    {
        private static readonly float EffectiveImageScalingSize = Level.size - (Level.border * 2);
        private static readonly float FullImageSize = Level.size;
        public readonly float MaxHeight;
        public readonly float MinHeight;
        protected readonly float _multiplier;
        public float CoordinateMultiplier { get => _multiplier; }
        public readonly Vector2 Center;
        public Vector3 Center3D
        {
            get => new Vector3(Center.x, Mathf.Max(F.GetTerrainHeightAt2DPoint(Center), MinHeight), Center.y);
        }
        public Vector3 Center3DAbove
        {
            get => new Vector3(Center.x, Mathf.Max(F.GetTerrainHeightAt2DPoint(Center, above: F.SPAWN_HEIGHT_ABOVE_GROUND), MinHeight + F.SPAWN_HEIGHT_ABOVE_GROUND), Center.y);
        }
        public Zone InverseZone;
        protected bool SucessfullyParsed = false;
        public readonly ZoneData data;
        protected float spacing = -1;
        protected int perline = 10;
        public Vector2[] _particleSpawnPoints;
        public readonly string Name;
        public Vector2 BoundsTLPos;
        public Vector2 BoundsSize;
        public float BoundsArea;
        public abstract bool IsInside(Vector2 location);
        public abstract bool IsInside(Vector3 location);
        public virtual string Dump() => $"{Name}: {data.type}, {data.data}. ({Center}).{(MaxHeight != -1 ? $" Max Height: {MaxHeight}." : string.Empty)}{(MinHeight != -1 ? $" Min Height: {MinHeight}." : string.Empty)}";
        public IEnumerator<SteamPlayer> EnumerateClients()
        {
            for (int i = 0; i < Provider.clients.Count; i++)
            {
                SteamPlayer player = Provider.clients[i];
                if (IsInside(player.player.transform.position))
                    yield return player;
            }
        }
        public override string ToString() => Dump();
        public abstract Vector2[] GetParticleSpawnPoints(out Vector2[] corners, out Vector2 center, int perline = -1, float spacing = -1);
        public abstract void Init();
        public Zone(Vector2 Center, ZoneData data, bool useMapSizeMultiplier, string Name, float maxHeight, float minHeight, bool isInverse = false)
        {
            this.Name = Name;
            this._multiplier = useMapSizeMultiplier ? EffectiveImageScalingSize / FullImageSize : 1.0f;
            this.Center = new Vector2(Center.x * _multiplier, Center.y * _multiplier);
            this.data = data;
            this.MaxHeight = maxHeight;
            this.MinHeight = minHeight;
            Init();
        }
        public bool IsInsideBounds(Vector2 location)
        {
            return location.x > BoundsTLPos.x && location.x < BoundsTLPos.x + BoundsSize.x && location.y > BoundsTLPos.y && location.y < BoundsTLPos.y + BoundsSize.y;
        }
        public bool IsInsideBounds(Vector3 location)
        {
            return location.x > BoundsTLPos.x && location.x < BoundsTLPos.x + BoundsSize.x && location.z > BoundsTLPos.y && location.z < BoundsTLPos.y + BoundsSize.y;
        }
    }
    public class RectZone : Zone
    {
        public Vector2 Size;
        public Line[] lines;
        public Vector2[] Corners;
        public RectZone RectInverseZone { get => (RectZone)InverseZone; }
        public RectZone(Vector2 Center, ZoneData data, bool useMapSizeMultiplier, string Name, float maxHeight, float minHeight, bool isInverse = false) :
            base(Center, data, useMapSizeMultiplier, Name, maxHeight, minHeight, isInverse)
        {
            if (!isInverse)
                this.InverseZone = new RectZone(Center, data, !useMapSizeMultiplier, Name, maxHeight, minHeight, true);
        }
        /// <param name="corners">Corners to spawn different points at.</param>
        /// <param name="spacing">Set to -1 to to use perline. Can not be &lt;0.1</param>
        /// <param name="perline">If <paramref name="spacing"/> <see cref="float.Equals(float)">==</see> -1, it equally divides the spacing for each line to have <paramref name="perline"/> spawn points per line. Can not be zero.</param>
        /// <returns></returns>
        public override Vector2[] GetParticleSpawnPoints(out Vector2[] corners, out Vector2 center, int perline = -1, float spacing = -1f)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            corners = Corners;
            center = Center;
            if (this.spacing == spacing && this.perline == perline && _particleSpawnPoints != null) return _particleSpawnPoints;
            if (spacing >= 0.1f) this.spacing = spacing;
            if (perline != 0f && perline != -1f) this.perline = perline;
            List<Vector2> rtnSpawnPoints = new List<Vector2>();
            foreach (Line line in lines)
            {
                if (line.length == 0) continue;
                float distance;
                if (this.spacing == -1f) distance = line.length / this.perline;
                else distance = line.NormalizeSpacing(this.spacing);
                if (distance != 0) // prevent infinite loops
                    for (float i = distance; i < line.length; i += distance)
                    {
                        rtnSpawnPoints.Add(line.GetPointFromDistanceFromPt1(i));
                    }
            }
            _particleSpawnPoints = rtnSpawnPoints.ToArray();
            return _particleSpawnPoints;
        }
        public override void Init()
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            string[] size = data.data.Split(',');
            if (size.Length != 2)
            {
                L.LogError($"Zone at ({Center.x}, {Center.y}), {Name}, has invalid rectangle data. Format is: \"size x,size y\".");
                return;
            }
            else
            {
                if (float.TryParse(size[0], NumberStyles.Any, Data.Locale, out float SizeX) && float.TryParse(size[1], NumberStyles.Any, Data.Locale, out float SizeY))
                {
                    Size = new Vector2(SizeX * _multiplier, SizeY * _multiplier);
                    Corners = new Vector2[4]
                    {
                        new Vector2(Center.x - Size.x / 2, Center.y - Size.y / 2), //tl
                        new Vector2(Center.x + Size.x / 2, Center.y - Size.y / 2), //tr
                        new Vector2(Center.x + Size.x / 2, Center.y + Size.y / 2), //br
                        new Vector2(Center.x - Size.x / 2, Center.y + Size.y / 2)  //bl
                    };
                    BoundsTLPos = Corners[0];
                    BoundsSize = Size;
                    BoundsArea = Size.x * Size.y;
                    lines = new Line[4]
                    {
                        new Line(Corners[0], Corners[1]), // tl -> tr
                        new Line(Corners[1], Corners[2]), // tr -> br
                        new Line(Corners[2], Corners[3]), // br -> bl
                        new Line(Corners[3], Corners[0]), // bl -> tl
                    };
                    GetParticleSpawnPoints(out _, out _, 5, 15);
                    SucessfullyParsed = true;
                }
                else
                {
                    L.LogError($"Zone at ({Center.x}, {Center.y}) has invalid rectangle data. Format is: \"size x,size y\".");
                    return;
                }
            }
        }

        public override bool IsInside(Vector2 location)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            return location.x > Center.x - Size.x / 2 && location.x < Center.x + Size.x / 2 && location.y > Center.y - Size.y / 2 && location.y < Center.y + Size.y / 2;
        }
        public override bool IsInside(Vector3 location)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            return (MinHeight == -1 || location.y >= MinHeight) && (MaxHeight == -1 || location.y <= MaxHeight) &&
                   location.x > Center.x - Size.x / 2 && location.x < Center.x + Size.x / 2 && location.z > Center.y - Size.y / 2 && location.z < Center.y + Size.y / 2;
        }
        public override string Dump() => $"{base.Dump()}. Size: {Size.x}x{Size.y}";
    }
    public class CircleZone : Zone
    {
        public float Radius;
        public CircleZone CircleInverseZone { get => (CircleZone)InverseZone; }
        public CircleZone(Vector2 Center, ZoneData data, bool useMapSizeMultiplier, string Name, float maxHeight, float minHeight, bool isInverse = false) :
            base(Center, data, useMapSizeMultiplier, Name, maxHeight, minHeight, isInverse)
        {
            if (!isInverse)
                this.InverseZone = new CircleZone(Center, data, !useMapSizeMultiplier, Name, maxHeight, minHeight, true);
        }

        public override Vector2[] GetParticleSpawnPoints(out Vector2[] corners, out Vector2 center, int perline = -1, float spacing = -1)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            corners = new Vector2[0];
            center = Center;
            if (this.spacing == spacing && this.perline == perline && _particleSpawnPoints != null) return _particleSpawnPoints;
            float pi2F = 2f * Mathf.PI;
            float circumference = pi2F * Radius;
            if (spacing >= 0.1f)
            {
                this.spacing = spacing;
                int canfit = F.DivideRemainder(circumference, this.spacing, out int remainder);
                if (remainder != 0)
                {
                    if (remainder < this.spacing / 2)     // extend all others
                        this.spacing = circumference / canfit;
                    else                                  //add one more and subtend all others
                        this.spacing = circumference / (canfit + 1);
                }
            }
            if (perline != 0f && perline != -1f) this.perline = perline;
            List<Vector2> rtnSpawnPoints = new List<Vector2>();
            float angleRad;
            if (this.spacing == -1f) angleRad = circumference / this.perline / Radius;
            else angleRad = this.spacing / Radius;
            for (float i = 0; i < pi2F; i += angleRad)
            {
                rtnSpawnPoints.Add(new Vector2(Center.x + (Mathf.Cos(i) * Radius), Center.y + (Mathf.Sin(i) * Radius)));
            }
            _particleSpawnPoints = rtnSpawnPoints.ToArray();
            return _particleSpawnPoints;
        }
        public override void Init()
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            string[] size = data.data.Split(',');
            if (size.Length != 1)
            {
                L.LogError($"Zone at ({Center.x}, {Center.y}), {Name}, has invalid circle data. Format is: \"radius\".");
                return;
            }
            else
            {
                if (float.TryParse(size[0], NumberStyles.Any, Data.Locale, out float Rad))
                {
                    this.Radius = Rad * _multiplier;
                    GetParticleSpawnPoints(out _, out _, 36, 15); // every 10 degrees
                    BoundsTLPos = new Vector2(Center.x - Radius, Center.y - Radius);
                    BoundsSize = new Vector2(Radius * 2, Radius * 2);
                    BoundsArea = BoundsSize.x * BoundsSize.y;
                    SucessfullyParsed = true;
                }
                else
                {
                    L.LogError($"Zone at ({Center.x}, {Center.y}), {Name}, has invalid circle data. Format is: \"radius\".");
                    return;
                }
            }
        }

        public override bool IsInside(Vector2 location)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            //if (!IsInsideBounds(location)) return false;
            float difX = location.x - Center.x;
            float difY = location.y - Center.y;
            float sqrDistance = (difX * difX) + (difY * difY);
            return sqrDistance <= Radius * Radius;
        }
        public override bool IsInside(Vector3 location)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if ((MinHeight != -1 && location.y < MinHeight) || (MaxHeight != -1 && location.y > MaxHeight)) return false;
            //if (!IsInsideBounds(location)) return false;
            float difX = location.x - Center.x;
            float difY = location.z - Center.y;
            float sqrDistance = (difX * difX) + (difY * difY);
            return sqrDistance <= Radius * Radius;
        }
        public override string Dump() => $"{base.Dump()}. Radius: {Radius}";
    }
    public readonly struct Line
    {
        public readonly Vector2 pt1;
        public readonly Vector2 pt2;
        public readonly float m; // slope 
        public readonly float b; // y-inx
        public readonly float length;
        public Line(in Vector2 pt1, in Vector2 pt2)
        {
            this.pt1 = pt1;
            this.pt2 = pt2;
            this.m = (pt1.y - pt2.y) / (pt1.x - pt2.x);
            this.b = -1 * ((m * pt1.x) - pt1.y);
            this.length = Vector2.Distance(pt1, pt2);
        }
        public readonly float NormalizeSpacing(float baseSpacing)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            int canfit = F.DivideRemainder(length, baseSpacing, out int remainder);
            if (remainder == 0) return baseSpacing;
            if (remainder < baseSpacing / 2)     // extend all others
                return length / canfit;
            else                                //add one more and subtend all others
                return length / (canfit + 1);
        }
        public readonly bool IsIntersecting(float playerY, float playerX)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (playerY < Mathf.Min(pt1.y, pt2.y) || playerY >= Mathf.Max(pt1.y, pt2.y)) return false; // if input value is out of vertical range of line
            if (pt1.x == pt2.x) return pt1.x >= playerX; // checks for undefined sloped (a completely vertical line)
            float x = GetX(playerY); // solve for y
            return x >= playerX; // if output value is in front of player
        }
        public readonly float GetX(float y) => (y - b) / m; // inverse slope function
        public readonly float GetY(float x) => m * x + b;   // slope function
        public static Vector2 AvgAllPoints(Vector2[] Points)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            float xTotal = 0;
            float yTotal = 0;
            int i;
            for (i = 0; i < Points.Length; i++)
            {
                xTotal += Points[i].x;
                yTotal += Points[i].y;
            }
            return new Vector2(xTotal / i, yTotal / i);
        }
        public readonly Vector2 GetPointFromPercentFromPt1(float percent0to1)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (pt1.x == pt2.x) return new Vector2(pt1.x, pt1.y + (pt2.y - pt1.y) * percent0to1);
            float x = pt1.x + ((pt2.x - pt1.x) * percent0to1);
            return new Vector2(x, GetY(x));
        }
        public readonly Vector2 GetPointFromDistanceFromPt1(float distance) => GetPointFromPercentFromPt1(distance / length);
        public readonly Vector2 GetPointFromPercentFromPt2(float percent0to1)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (pt2.x == pt1.x) return new Vector2(pt2.x, pt2.y + (pt1.y - pt2.y) * percent0to1);
            float x = pt2.x + ((pt1.x - pt2.x) * percent0to1);
            return new Vector2(x, GetY(x));
        }
        public readonly Vector2 GetPointFromDistanceFromPt2(float distance) => GetPointFromPercentFromPt2(distance / length);
    }
    public class PolygonZone : Zone
    {
        public Vector2[] Points;
        public Line[] Lines;
        public float Width;
        public PolygonZone PolygonInverseZone { get => (PolygonZone)InverseZone; }
        /// <param name="size">Size of bounds rectangle</param>
        /// <returns>Top left corner of bounds rectangle</returns>
        public static Vector2 GetBounds(Vector2[] points, out Vector2 size)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            float? maxX = null, maxY = null, minX = null, minY = null;
            if ((points ?? throw new NullReferenceException("EXCEPTION_NO_POINTS_GIVEN")).Length == 0) throw new NullReferenceException("EXCEPTION_NO_POINTS_GIVEN");
            if (points.Length == 1)
            {
                size = new Vector2(0, 0);
                return new Vector2(points[0].x, points[0].y);
            }
            for (int i = 0; i < points.Length; i++)
            {
                ref Vector2 point = ref points[i];
                if (!maxX.HasValue || maxX.Value > point.x) maxX = point.x;
                if (!maxY.HasValue || maxY.Value > point.y) maxY = point.y;
                if (!minX.HasValue || minX.Value < point.x) minX = point.x;
                if (!minY.HasValue || minY.Value < point.y) minY = point.y;
            }
            if (maxX.HasValue && maxY.HasValue && minX.HasValue && maxX.HasValue)
            {
                size = new Vector2(maxX.Value - minX.Value, maxY.Value - minY.Value);
                return new Vector2(minX.Value, minY.Value);
            }
            else throw new NullReferenceException("EXCEPTION_NO_POINTS_GIVEN");
        }
        public PolygonZone(Vector2 Center, ZoneData data, bool useMapSizeMultiplier, string Name, float maxHeight, float minHeight, bool isInverse = false) :
            base(Center, data, useMapSizeMultiplier, Name, maxHeight, minHeight, isInverse)
        {
            if (!isInverse)
                this.InverseZone = new PolygonZone(Center, data, !useMapSizeMultiplier, Name, maxHeight, minHeight, true);
        }

        /// <param name="corners">Corners to spawn different points at.</param>
        /// <param name="spacing">Set to -1 to to use perline. Can not be &lt;0.1</param>
        /// <param name="perline">If spacing is negative 1, it equally divides the spacing for each line to have &lt;perline&gt; spawn points per line. Can not be zero.</param>
        /// <returns></returns>
        public override Vector2[] GetParticleSpawnPoints(out Vector2[] corners, out Vector2 center, int perline = -1, float spacing = -1f)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            corners = Points;
            center = Center;
            if (this.spacing == spacing && this.perline == perline && _particleSpawnPoints != null) return _particleSpawnPoints;
            if (spacing >= 0.1f) this.spacing = spacing;
            if (perline != 0f && perline != -1f) this.perline = perline;
            List<Vector2> rtnSpawnPoints = new List<Vector2>();
            foreach (Line line in Lines)
            {
                if (line.length == 0) continue;
                float distance;
                if (this.spacing == -1f) distance = line.length / this.perline;
                else distance = line.NormalizeSpacing(this.spacing);
                for (float i = distance; i < line.length; i += distance)
                {
                    rtnSpawnPoints.Add(line.GetPointFromDistanceFromPt1(i));
                }
            }
            _particleSpawnPoints = rtnSpawnPoints.ToArray();
            return _particleSpawnPoints;
        }

        public override void Init()
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            string[] size = data.data.Split(',');
            if (size.Length < 6 || size.Length % 2 == 1)
            {
                L.LogError($"Zone at ({Center.x}, {Center.y}), {Name}, has invalid polygon data - \"{Name}\". Format is: \"x1,y1,x2,y2,x3,y3,...\" with at least 3 points.\n{data.data}");
                return;
            }
            Points = new Vector2[size.Length / 2];
            for (int i = 0; i < size.Length; i += 2)
            {
                if (float.TryParse(size[i], NumberStyles.Any, Data.Locale, out float x) && float.TryParse(size[i + 1], NumberStyles.Any, Data.Locale, out float y))
                {
                    Points[i / 2] = new Vector2(x * _multiplier, y * _multiplier);
                }
                else
                {
                    L.LogError($"Zone at ({Center.x}, {Center.y}), {Name}, has invalid polygon data - \"{Name}\". Format is: \"x1,y1,x2,y2,x3,y3,...\" with at least 3 points.\n{data.data}");
                    L.LogError($"Couldn't parse ({size[i]}, {size[i + 1]}).");
                    return;
                }
            }
            Lines = new Line[Points.Length];
            for (int i = 0; i < Points.Length; i++)
                Lines[i] = new Line(Points[i], Points[i == Points.Length - 1 ? 0 : i + 1]);
            GetParticleSpawnPoints(out _, out _, 5, 15);
            try
            {
                BoundsTLPos = GetBounds(Points, out BoundsSize);
            }
            catch (NullReferenceException)
            {
                BoundsSize = new Vector2(0, 0);
                BoundsTLPos = Center;
            }
            BoundsArea = BoundsSize.x * BoundsSize.y;
            SucessfullyParsed = true;
        }

        public override bool IsInside(Vector2 location)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (!this.SucessfullyParsed)
            {
                L.LogError(Name + " DIDN'T PARSE CORRECTLY");
                return false;
            }
            // TODO: Bounds check
            //if (!IsInsideBounds(location)) return false;
            int intersects = 0;
            for (int i = 0; i < Lines.Length; i++)
            {
                if (Lines[i].IsIntersecting(location.y, location.x)) intersects++;
            }
            if (intersects % 2 == 1) return true; // is odd
            else return false;
        }
        public override bool IsInside(Vector3 location)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if ((MinHeight != -1 && location.y < MinHeight) || (MaxHeight != -1 && location.y > MaxHeight)) return false;
            if (!this.SucessfullyParsed)
            {
                L.LogError(Name + " DIDN'T PARSE CORRECTLY");
                return false;
            }
            //if (!IsInsideBounds(location)) return false;
            int intersects = 0;
            for (int i = 0; i < Lines.Length; i++)
            {
                if (Lines[i].IsIntersecting(location.z, location.x)) intersects++;
            }
            if (intersects % 2 == 1) return true; // is odd
            else return false;
        }
        public override string Dump()
        {
            StringBuilder sb = new StringBuilder($"{base.Dump()}\n");
            for (int i = 0; i < Lines.Length; i++)
            {
                sb.Append($"Line {i + 1}: ({Lines[i].pt1.x}, {Lines[i].pt1.y}) to ({Lines[i].pt2.x}, {Lines[i].pt2.y}).\n");
            }
            return sb.ToString();
        }
    }
    public struct ZoneData
    {
        public string type;
        public string data;
        [JsonConstructor]
        public ZoneData(string type, string data)
        {
            this.type = type;
            this.data = data;
        }
    }
}
