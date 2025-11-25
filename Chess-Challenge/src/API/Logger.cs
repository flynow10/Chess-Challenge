using System;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;

namespace ChessChallenge.API;

public static class Logger
{
    public enum LogLevel
    {
        None = 0,
        PV = 1,
        Full = 2
    }

    public static LogLevel Level = LogLevel.Full;

    public static void Write(LogLevel level, bool value)
    {
        if(CheckLevel(level))
            Console.Write(value);
    }

    public static void Write(LogLevel level, char value)
    {
        if(CheckLevel(level))
            Console.Write(value);
    }

    public static void Write(LogLevel level, char[]? buffer)
    {
        if(CheckLevel(level))
            Console.Write(buffer);
    }

    public static void Write(LogLevel level, char[] buffer, int index, int count)
    {
        if (CheckLevel(level))
            Console.Write(buffer, index, count);
    }

    public static void Write(LogLevel level, double value)
    {
        if(CheckLevel(level))
            Console.Write(value);
    }

    public static void Write(LogLevel level, decimal value)
    {
        if(CheckLevel(level))
            Console.Write(value);
    }

    public static void Write(LogLevel level, int value)
    {
        if(CheckLevel(level))
            Console.Write(value);
    }

    public static void Write(LogLevel level, uint value)
    {
        if(CheckLevel(level))
            Console.Write(value);
    }

    public static void Write(LogLevel level, long value)
    {
        if(CheckLevel(level))
            Console.Write(value);
    }

    public static void Write(LogLevel level, ulong value)
    {
        if(CheckLevel(level))
            Console.Write(value);
    }

    public static void Write(LogLevel level, object? value)
    {
        if(CheckLevel(level))
            Console.Write(value);
    }

    public static void Write(LogLevel level, string? value)
    {
        if(CheckLevel(level))
            Console.Write(value);
    }

    public static void Write(LogLevel level, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0)
    {
        if(CheckLevel(level))
            Console.Write(format, arg0);
    }
    
    public static void Write(LogLevel level, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1)
    {
        if(CheckLevel(level))
            Console.Write(format, arg0, arg1);
    }

    public static void Write(LogLevel level, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0,
        object? arg1, object? arg2)
    {
        if (CheckLevel(level))
            Console.Write(format, arg0, arg1, arg2);
    }

    public static void Write(LogLevel level, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[]? arg)
    {
        if (CheckLevel(level))
        {
            if(arg == null)
                Console.Write(format, null, null);
            else
                Console.Write(format, arg);
        }
    }

    public static void WriteLine(LogLevel level, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0)
    {
        if(CheckLevel(level))
            Console.WriteLine(format, arg0);
    }
    
    public static void WriteLine(LogLevel level, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1)
    {
        if(CheckLevel(level))
            Console.WriteLine(format, arg0, arg1);
    }

    public static void WriteLine(LogLevel level, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0,
        object? arg1, object? arg2)
    {
        if (CheckLevel(level))
            Console.WriteLine(format, arg0, arg1, arg2);
    }

    public static void WriteLine(LogLevel level, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[]? arg)
    {
        if (CheckLevel(level))
        {
            if(arg == null)
                Console.WriteLine(format, null, null);
            else
                Console.WriteLine(format, arg);
        }
    }
    
    public static void WriteLine(LogLevel level, bool value)
    {
        if(CheckLevel(level))
            Console.WriteLine(value);
    }

    public static void WriteLine(LogLevel level, char value)
    {
        if(CheckLevel(level))
            Console.WriteLine(value);
    }

    public static void WriteLine(LogLevel level, char[]? buffer)
    {
        if(CheckLevel(level))
            Console.WriteLine(buffer);
    }

    public static void WriteLine(LogLevel level, char[] buffer, int index, int count)
    {
        if (CheckLevel(level))
            Console.WriteLine(buffer, index, count);
    }

    public static void WriteLine(LogLevel level, double value)
    {
        if(CheckLevel(level))
            Console.WriteLine(value);
    }

    public static void WriteLine(LogLevel level, decimal value)
    {
        if(CheckLevel(level))
            Console.WriteLine(value);
    }

    public static void WriteLine(LogLevel level, int value)
    {
        if(CheckLevel(level))
            Console.WriteLine(value);
    }

    public static void WriteLine(LogLevel level, uint value)
    {
        if(CheckLevel(level))
            Console.WriteLine(value);
    }

    public static void WriteLine(LogLevel level, long value)
    {
        if(CheckLevel(level))
            Console.WriteLine(value);
    }

    public static void WriteLine(LogLevel level, ulong value)
    {
        if(CheckLevel(level))
            Console.WriteLine(value);
    }

    public static void WriteLine(LogLevel level, object? value)
    {
        if(CheckLevel(level))
            Console.WriteLine(value);
    }

    public static void WriteLine(LogLevel level, string? value)
    {
        if(CheckLevel(level))
            Console.WriteLine(value);
    }
    
    private static bool CheckLevel(LogLevel level) => Level >= level;
}