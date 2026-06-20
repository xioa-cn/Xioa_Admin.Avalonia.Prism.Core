using System;

namespace Ava.Xioa.Common.Models;

public class ConfigurationErrorsException : Exception
{
    public ConfigurationErrorsException(string message) : base(message)
    {
    }
}