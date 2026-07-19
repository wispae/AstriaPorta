using System;
using System.Collections.Generic;
using System.Text;

namespace AstriaPorta.Util;

public static class AddressUtils
{
    public static readonly string ValidGlyphsLowercase = "0123456789abcdefghijklmnopqrstuvwxyz";
    public static readonly string ValidGlyphsUppercase = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public static string FormatAddressString(string s)
    {
        string sanitized = SanitizeAddressString(s);

        if (sanitized.Length == 0)
            return string.Empty;

        int length = sanitized.Length;
        while (length > 0 && sanitized[length - 1] == '0')
        {
            length--;
        }

        if (length == 0)
            return string.Empty;

        var sb = new StringBuilder(length + (length / 3));
        for (int i = 0; i < length; i++)
        {
            if (i > 0 && i%3 == 0)
            {
                sb.Append('-');
            }

            sb.Append(sanitized[i]);
        }

        return sb.ToString().ToUpperInvariant();
    }

    public static string FormatAddress(IStargateAddress a)
    {
        byte[] glyphs = a.AddressCoordinates.Glyphs;
        if (glyphs == null || glyphs.Length == 0)
            return string.Empty;

        int length = glyphs.Length;
        while (length > 0 && glyphs[length - 1] == 0)
        {
            length--;
        }

        if (length == 0)
            return string.Empty;

        var sb = new StringBuilder(length + (length / 3));
        for (int i = 0; i < length; i++)
        {
            if (i > 0 && i%3 == 0)
            {
                sb.Append('-');
            }

            sb.Append(GlyphToUppercaseChar(glyphs[i]));
        }

        return sb.ToString();
    }

    public static char GlyphToLowercaseChar(byte glyph)
    {
        return ValidGlyphsLowercase[glyph % 36];
    }

    public static char GlyphToUppercaseChar(byte glyph)
    {
        return ValidGlyphsUppercase[glyph % 36];
    }

    public static bool IsValidAddressString(string s)
    {
        var s2 = s.ToLower();
        int validCharacters = 0;

        for (int i = 0; i < s.Length; i++)
        {
            if (ValidGlyphsLowercase.Contains(s2[i]))
                validCharacters++;
        }

        return 7 <= validCharacters && validCharacters <= 9;
    }

    public static string SanitizeAddressString(string s)
    {
        string output = "";

        var s2 = s.ToLower();
        for (int i = 0; i < s2.Length; i++)
        {
            if (ValidGlyphsLowercase.Contains(s2[i])) output += s2[i];
        }

        if (s2.Length > 9)
        {
            s2 = s2.Substring(0, 9);
        }

        return output;
    }

    public static byte[] StringAddressToBytes(string s)
    {
        var s2 = s.ToLower();
        byte[] glyphs = new byte[s2.Length];

        for (byte i = 0; i < s2.Length; i++)
        {
            for (byte j = 0; j < ValidGlyphsLowercase.Length; j++)
            {
                if (s2[i] == ValidGlyphsLowercase[j])
                {
                    glyphs[i] = j;
                    break;
                }
            }
        }

        return glyphs;
    }
}
