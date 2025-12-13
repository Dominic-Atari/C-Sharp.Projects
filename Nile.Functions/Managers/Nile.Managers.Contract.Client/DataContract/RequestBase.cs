using System.Text.Json.Serialization;
using Nile.Managers.DataContract.HealthCheck;
using Nile.Managers.HealthCheck;

namespace Nile.Managers.Contract.Client.DataContract;

/// <summary>
/// Base class for all request objects.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="TResponse"></typeparam>
/// <typeparam name="TManager"></typeparam>
/// <typeparam name="TRequest"></typeparam>
[JsonDerivedType(typeof(RequestBase), typeDiscriminator: "base")]
[JsonDerivedType(typeof(HealthCheckRequest), typeDiscriminator: nameof(HealthCheckRequest))]
[JsonDerivedType(typeof(TestMessage), typeDiscriminator: nameof(TestMessage))]
public class RequestBase : Nile.Common.InternalDTOs.RequestBase
{
}