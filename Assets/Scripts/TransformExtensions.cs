using UnityEngine;
using System.Text;

public static class TransformExtensions
{
    // 전체 루트 기준
    public static string GetPath(this Transform current)
    {
        if (current == null)
            return string.Empty;

        StringBuilder path = new StringBuilder(current.name);

        while (current.parent != null)
        {
            current = current.parent;
            path.Insert(0, current.name + "/");
        }

        return path.ToString();
    }

    // 특정 루트 기준
    public static string GetPath(this Transform current, Transform root)
    {
        if (current == null)
            return string.Empty;

        StringBuilder path = new StringBuilder(current.name);

        while (current.parent != null && current.parent != root)
        {
            current = current.parent;
            path.Insert(0, current.name + "/");
        }

        if (root != null)
            path.Insert(0, root.name + "/");

        return path.ToString();
    }
}
