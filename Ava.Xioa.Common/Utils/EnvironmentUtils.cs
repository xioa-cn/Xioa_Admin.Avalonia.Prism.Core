using System;
using Ava.Xioa.Common.Services;

namespace Ava.Xioa.Common.Utils;

public static class EnvironmentUtils
{
    public static void EnvironmentExit(this IExitService exitService, int code)
    {
        exitService.Exit();
        Environment.Exit(code);
    }

    public static void Exit(int code, params IExitService[]? exitService)
    {
        if (exitService is not null)
        {
            foreach (var exitItem in exitService)
            {
                exitItem.Exit();
            }
        }

        Environment.Exit(code);
    }
}