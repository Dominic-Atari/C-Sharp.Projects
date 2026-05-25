using N.LMS.Common.Interface.Request;

namespace N.LMS.Engine.Validation.Interface.Request;

/// <summary>
/// Default validate-request shape used when a manager has no specific rules registered for a
/// given inbound request type. Engines may pattern-match on <see cref="ValidateRequestBase.InboundRequest"/>
/// to apply checks; absent any match, the engine returns success.
/// </summary>
public sealed record GenericValidateRequest : ValidateRequestBase;
