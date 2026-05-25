namespace N.LMS.Common.Interface.Request;

public abstract record ValidateRequestBase : RequestBase
{
    /// <summary>
    /// The original Manager request that triggered the validation pass. Engines can switch on
    /// this to apply request-specific rules without losing the typed inbound payload.
    /// </summary>
    public RequestBase? InboundRequest { get; init; }
}
