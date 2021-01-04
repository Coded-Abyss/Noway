using System.Collections.Generic;

public static class Extensions {
    public static string AddCommas(this IEnumerable<string> iterator) {
        var ret = "";
        foreach(var s in iterator) {
            ret += s + ",";
        }
        return ret.Length == 0 ? "" : ret.Substring(0, ret.Length - 1);
    }
    public static string Between(this string s, string a, string b) {
        var index1 = s.IndexOf(a);
        var index2 = s.LastIndexOf(b);
        if(index1 == -1 || index2 == -1) return null;
        return s.Substring(index1 + a.Length, index2 - index1 - a.Length);
    }
    public static string Before(this string s, string a) {
        var index = s.IndexOf(a);
        if(index == -1) return s;
        return s.Substring(0, index);
    }
}