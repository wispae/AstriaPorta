using System;
using System.Collections.Generic;
using System.Text;

namespace AstriaPorta.Util;

public static class AddressUtils
{
    public static string SanitizeAddressString(string s)
    {
        string validGlyphs = "0123456789abcdefghijklmnopqrstuvwxyz";
        string output = "";

        if (s.Length > 9)
        {
            s = s.Substring(0, 9);
        }

        s = s.ToLower();
        for (int i = 0; i < s.Length; i++)
        {
            if (validGlyphs.Contains(s[i])) output += s[i];
        }

        return output;
    }

    public static byte[] StringAddressToBytes(string s)
    {
        string validGlyphs = "0123456789abcdefghijklmnopqrstuvwxyz";
        byte[] glyphs = new byte[s.Length];

        for (byte i = 0; i < s.Length; i++)
        {
            for (byte j = 0; j < validGlyphs.Length; j++)
            {
                if (s[i] == validGlyphs[j])
                {
                    glyphs[i] = j;
                    break;
                }
            }
        }

        return glyphs;
    }
}
