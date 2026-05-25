using N.LMS.Common.Interface.Request;

namespace N.LMS.Manager.Learning.Interface.Request;

public abstract record LearningRequestBase    : RequestBase;
public abstract record LearningLoadRequestBase  : LoadRequestBase;
public abstract record LearningStoreRequestBase : StoreRequestBase;
public abstract record LearningHealthCheckRequestBase : HealthCheckRequestBase;
