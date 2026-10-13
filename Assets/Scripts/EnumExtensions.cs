
using System;

public static class EnumExtensions
{
    public static T Next<T>(this T src) where T: Enum
    {
        T[] arr = (T[])Enum.GetValues(src.GetType());
        int j = Array.IndexOf<T>(arr, src) + 1;
        return (arr.Length==j) ? arr[0] : arr[j];
    }

    public static T Previous<T>(this T src) where T : Enum
    {
        T[] arr = (T[])Enum.GetValues(src.GetType());
        int j = Array.IndexOf<T>(arr, src) - 1;
        return (j < 0) ? arr[^1] : arr[j];
    }
}
