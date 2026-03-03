using System;
using System.Collections.Generic;
using UnityEngine;

namespace TurnBasedTactics.Grid
{
    /// <summary>
    /// Axial hex coordinate for flat-top hex grid.
    /// Uses (q, r) axial system with implicit s = -q - r.
    /// </summary>
    public readonly struct HexCoord : IEquatable<HexCoord>
    {
        public readonly int Q;
        public readonly int R;
        public int S => -Q - R;

        public static HexCoord Origin => new HexCoord(0, 0);

        private static readonly HexCoord[] DirectionOffsets =
        {
            new HexCoord(+1,  0), // E
            new HexCoord(+1, -1), // NE
            new HexCoord( 0, -1), // NW
            new HexCoord(-1,  0), // W
            new HexCoord(-1, +1), // SW
            new HexCoord( 0, +1), // SE
        };

        private const float Sqrt3 = 1.7320508f;

        public HexCoord(int q, int r)
        {
            Q = q;
            R = r;
        }

        // --- Neighbors ---

        public HexCoord GetNeighbor(HexDirection dir)
        {
            var off = DirectionOffsets[(int)dir];
            return new HexCoord(Q + off.Q, R + off.R);
        }

        public void GetNeighbors(List<HexCoord> results)
        {
            results.Clear();
            for (int i = 0; i < 6; i++)
            {
                var off = DirectionOffsets[i];
                results.Add(new HexCoord(Q + off.Q, R + off.R));
            }
        }

        // --- Distance ---

        public int DistanceTo(HexCoord other)
        {
            int dq = Math.Abs(Q - other.Q);
            int dr = Math.Abs(R - other.R);
            int ds = Math.Abs(S - other.S);
            return Math.Max(dq, Math.Max(dr, ds));
        }

        // --- Ring & Range ---

        public static List<HexCoord> GetRing(HexCoord center, int radius)
        {
            var results = new List<HexCoord>();
            if (radius <= 0)
            {
                results.Add(center);
                return results;
            }

            var coord = new HexCoord(
                center.Q + DirectionOffsets[4].Q * radius,
                center.R + DirectionOffsets[4].R * radius);

            for (int dir = 0; dir < 6; dir++)
            {
                for (int step = 0; step < radius; step++)
                {
                    results.Add(coord);
                    coord = coord.GetNeighbor((HexDirection)dir);
                }
            }
            return results;
        }

        public static List<HexCoord> GetRange(HexCoord center, int range)
        {
            var results = new List<HexCoord>();
            for (int q = -range; q <= range; q++)
            {
                int rMin = Math.Max(-range, -q - range);
                int rMax = Math.Min(range, -q + range);
                for (int r = rMin; r <= rMax; r++)
                {
                    results.Add(new HexCoord(center.Q + q, center.R + r));
                }
            }
            return results;
        }

        // --- Hex Line (for LoS / cover checks) ---

        public List<HexCoord> LineTo(HexCoord target)
        {
            int dist = DistanceTo(target);
            var results = new List<HexCoord>(dist + 1);
            if (dist == 0)
            {
                results.Add(this);
                return results;
            }

            float invDist = 1f / dist;
            for (int i = 0; i <= dist; i++)
            {
                float t = i * invDist;
                float fq = Q + (target.Q - Q) * t;
                float fr = R + (target.R - R) * t;
                float fs = S + (target.S - S) * t;
                results.Add(CubeRound(fq, fr, fs));
            }
            return results;
        }

        // --- World Conversion (flat-top hex) ---

        public static Vector3 HexToWorld(HexCoord coord, float outerRadius, Vector3 origin)
        {
            float x = outerRadius * 1.5f * coord.Q;
            float z = outerRadius * Sqrt3 * (coord.R + coord.Q * 0.5f);
            return new Vector3(origin.x + x, origin.y, origin.z + z);
        }

        public static HexCoord WorldToHex(Vector3 worldPos, float outerRadius, Vector3 origin)
        {
            float x = worldPos.x - origin.x;
            float z = worldPos.z - origin.z;

            float fq = (2f / 3f * x) / outerRadius;
            float fr = (-1f / 3f * x + Sqrt3 / 3f * z) / outerRadius;
            float fs = -fq - fr;

            return CubeRound(fq, fr, fs);
        }

        // --- Cube Rounding ---

        private static HexCoord CubeRound(float fq, float fr, float fs)
        {
            int q = Mathf.RoundToInt(fq);
            int r = Mathf.RoundToInt(fr);
            int s = Mathf.RoundToInt(fs);

            float dq = Mathf.Abs(q - fq);
            float dr = Mathf.Abs(r - fr);
            float ds = Mathf.Abs(s - fs);

            if (dq > dr && dq > ds)
                q = -r - s;
            else if (dr > ds)
                r = -q - s;

            return new HexCoord(q, r);
        }

        // --- Equality ---

        public bool Equals(HexCoord other) => Q == other.Q && R == other.R;
        public override bool Equals(object obj) => obj is HexCoord other && Equals(other);
        public override int GetHashCode() => Q * 397 ^ R;
        public static bool operator ==(HexCoord a, HexCoord b) => a.Q == b.Q && a.R == b.R;
        public static bool operator !=(HexCoord a, HexCoord b) => a.Q != b.Q || a.R != b.R;
        public static HexCoord operator +(HexCoord a, HexCoord b) => new HexCoord(a.Q + b.Q, a.R + b.R);
        public static HexCoord operator -(HexCoord a, HexCoord b) => new HexCoord(a.Q - b.Q, a.R - b.R);

        public override string ToString() => $"({Q}, {R})";
    }
}