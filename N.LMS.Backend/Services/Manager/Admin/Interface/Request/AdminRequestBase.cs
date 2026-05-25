using N.LMS.Common.Interface.Request;

namespace N.LMS.Manager.Admin.Interface.Request;

public abstract record AdminRequestBase : RequestBase;

public abstract record AdminLoadRequestBase     : LoadRequestBase;
public abstract record AdminStoreRequestBase    : StoreRequestBase;
public abstract record AdminDeleteRequestBase   : DeleteRequestBase;
public abstract record AdminHealthCheckRequestBase : HealthCheckRequestBase;
