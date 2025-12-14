using Microsoft.CodeAnalysis;
using System;
using System.Reflection;

namespace Ludamo.Cache.SourceGenerator;

public record struct WrappedInfo
{
    public WrappedInfo()
    {
    }

    //public readonly bool Equals(WrappedInfo other)
    //{
    //    return Accessibility == other.Accessibility
    //           && Name == other.Name
    //           && Namespace == other.Namespace
    //           && MethodName == other.MethodName
    //           && ReturnType == other.ReturnType;
    //}

    //public readonly override int GetHashCode()
    //{
    //    unchecked
    //    {
    //        var hashCode = Accessibility.GetHashCode();
    //        hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
    //        hashCode = (hashCode * 397) ^ (Namespace != null ? Namespace.GetHashCode() : 0);
    //        hashCode = (hashCode * 397) ^ (MethodName != null ? MethodName.GetHashCode() : 0);
    //        hashCode = (hashCode * 397) ^ (ReturnType != null ? ReturnType.GetHashCode() : 0);

    //        return hashCode;
    //    }
    //}

    /// <summary>
    /// Gets or sets the accessibility level for the associated element.
    /// </summary>
    public Accessibility? Accessibility { get; set; }

    /// <summary>
    /// Gets or sets the name from the containing type could be class or interface name for example.
    /// </summary>
    public string? Name { get; set; }

    public readonly string? DecoratedName
    {
        get
        {
            if (string.IsNullOrEmpty(Name))
            {
                return null;
            }
            if (Name!.StartsWith("I") && Name.Length > 1 && char.IsUpper(Name[1]))
            {
                // Avoid using range/index syntax for compatibility with older frameworks
                return $"{Name.Substring(1)}CacheDecorator";
            }
            return $"{Name}CacheDecorator";
        }
    }

    /// <summary>
    /// Gets or sets the namespace associated with the current wrapped type.
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets the name of the network interface associated with this instance.
    /// </summary>
    public string? InterfaceName { get; set; }

    /// <summary>
    /// All decorated methods for the containing type. Using an array makes it simple to enumerate
    /// and produce a single partial class with multiple generated methods.
    /// </summary>
    public MethodDetail[] Methods { get; set; } = [];
}

public record struct MethodDetail
{
    /// <summary>
    /// Gets or sets the name of the method to be invoked.
    /// </summary>
    public string? MethodName { get; set; }

    /// <summary>
    /// Gets or sets the name of the return type for the associated method or operation.
    /// </summary>
    public string? ReturnType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the operation is performed asynchronously.
    /// </summary>
    public bool IsAsync { get; set; }

    /// <summary>
    /// Gets the specific return type associated with the current instance.
    /// </summary>
    public string SpecificReturnType { get; set; }

    /// <summary>
    /// Gets or sets the expiration time, in seconds, for the associated resource or operation.
    /// </summary>
    /// <remarks>If set to <see langword="null"/>, the resource does not expire automatically. The value must
    /// be a positive integer if specified.</remarks>
    public int? ExpiresInSeconds { get; set; }

    /// <summary>
    /// Gets or sets the method parameter that represents the cache key.
    /// </summary>
    public MethodParameterInfo? KeyArgument { get; set; }
}

public record struct MethodParameterInfo
{
    /// <summary>
    /// Gets or sets the type of the parameter.
    /// </summary>
    public string? Type { get; set; }
    /// <summary>
    /// Gets or sets the name of the parameter.
    /// </summary>
    public string? Name { get; set; }
}
