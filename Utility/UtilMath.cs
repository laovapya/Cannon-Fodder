using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
public static class UtilMath
{
    public static bool IsInViewAngle(Vector3 position, Vector3 facing, Vector3 targetPos, float viewAngle)
    {
        Vector3 toTarget = (targetPos - position).normalized;
        Vector3 forward = facing.normalized;
        float angle = Vector3.Angle(forward, toTarget);
        return angle <= viewAngle * 0.5f;
    }

    public static float DragToSlowDownInDistance(float currentSpeed, float distance)
    {
        return (1 - Mathf.Exp(-currentSpeed / (distance / (Time.fixedDeltaTime)))) / Time.fixedDeltaTime;
    }
    public static float RoundFloat(float number, int decimals)
    {
        float multiplier = Mathf.Pow(10f, decimals);
        return Mathf.Round(number * multiplier) / multiplier;
    }
    public static bool SlopeAngleLessThan(Vector3 normal, float angle)
    {
        return normal.y > Mathf.Sin(angle * Mathf.Deg2Rad);
    }
    public static Vector3 ReflectionVector(Vector3 vector, Vector3 normal)
    {
        return vector - 2 * Vector3.Dot(vector, normal) * normal;
    }


    public static Vector3 Perpendicular2D(Vector3 v)
    {
        if (v.x == 0)
            return new Vector3(0, v.z, -v.y);
        if (v.y == 0)
            return new Vector3(v.z, 0, -v.x);
        if (v.z == 0)
            return new Vector3(v.y, -v.x, 0);
        return v;
    }

    public static Vector3 GetRandomXZDirection()
    {
        float a = UnityEngine.Random.Range(0, 360) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
    }
    public static Vector3 GetRandomDirection()
    {
        return new Vector3(UnityEngine.Random.Range(0, 100), UnityEngine.Random.Range(0, 100), UnityEngine.Random.Range(0, 100)).normalized;
    }


    public static Vector2 lineLineIntersection(Vector2 A, Vector2 B, Vector2 C, Vector2 D)
    {
        // Line AB represented as a1x + b1y = c1
        float a1 = B.y - A.y;
        float b1 = A.x - B.x;
        float c1 = a1 * (A.x) + b1 * (A.y);

        // Line CD represented as a2x + b2y = c2
        float a2 = D.y - C.y;
        float b2 = C.x - D.x;
        float c2 = a2 * (C.x) + b2 * (C.y);

        float determinant = a1 * b2 - a2 * b1;

        if (determinant == 0)
        {
            // The lines are parallel. This is simplified
            // by returning a pair of FLT_MAX
            return new Vector2(float.MaxValue, float.MaxValue);
        }
        else
        {
            float x = (b2 * c1 - b1 * c2) / determinant;
            float y = (a1 * c2 - a2 * c1) / determinant;
            return new Vector2(x, y);
        }
    }

    public static Vector2 Rotate2DVector(Vector2 vector, float angle)
    {
        float x = vector.x * Mathf.Cos(angle) - vector.y * Mathf.Sin(angle);
        float y = vector.x * Mathf.Sin(angle) + vector.y * Mathf.Cos(angle);
        return new Vector2(x, y);
    }

    public static Vector3 GetClosestLinePoint(Vector3 p0, Vector3 p1, Vector3 p)
    {
        Vector3 lineDir = (p1 - p0).normalized;
        Vector3 W = p - p0;
        return p0 + lineDir * Vector3.Dot(W, lineDir);
    }

    public static bool IsAngleMoreThan90(Vector3 v1, Vector3 v2)
    {
        return Vector3.Dot(v1, v2) < 0;
    }



    public static float GetAngleFromDirection(Vector2 dir)
    {
        dir.Normalize();
        float angle = MathF.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360;
        return angle;

    }

    public static Vector2 GetRandomCirclePoint(float minRadius, float maxRadius)
    {
        float angle = UnityEngine.Random.Range(0, 2 * Mathf.PI);
        float radius = UnityEngine.Random.Range(minRadius, maxRadius);
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }
    public static Vector2 GetRandomTilePosition(Vector2 tileCenter, float tileSize)
    {
        tileSize /= 2;
        return tileCenter + new Vector2(UnityEngine.Random.Range(-tileSize, tileSize), UnityEngine.Random.Range(-tileSize, tileSize));
    }

    public static Vector2 GetRandomPositionPlusPixel(Vector2 position, float pixelCount)
    {
        return position + new Vector2(UnityEngine.Random.Range(-pixelCount / 6.4f, pixelCount / 6.4f), UnityEngine.Random.Range(-pixelCount / 6.4f, pixelCount / 6.4f));
    }
}

