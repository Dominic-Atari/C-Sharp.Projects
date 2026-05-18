using System;

namespace Nile.Client.Functions.Common;

// Minimal attribute to tag Functions with a context type (parity with the traced project)
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ContextTypeAttribute : Attribute
{
    public Type Context { get; }
    public ContextTypeAttribute(Type context) => Context = context;
}

// Minimal context placeholder; extend with user/device claims as needed