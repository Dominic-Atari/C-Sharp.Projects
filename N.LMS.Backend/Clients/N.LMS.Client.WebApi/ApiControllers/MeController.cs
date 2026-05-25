using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using N.LMS.Client.WebApi.Extension;
using N.LMS.Client.WebApi.Response.Admin;
using N.LMS.Common.Service;
using N.LMS.Manager.Admin.Interface;
using AdminReq = N.LMS.Manager.Admin.Interface.Request;
using AdminRes = N.LMS.Manager.Admin.Interface.Result;

namespace N.LMS.Client.WebApi.ApiControllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class MeController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        using var proxy = new ServiceProxyGenerator(HttpContext.ToWebContext());

        var result = (AdminRes.MeLoadResult)await proxy.ProxyForService<IAdminManager>()
            .Load(new AdminReq.MeLoadRequest());

        return CreateActionResult(Mapper.Map<MeLoadResponse>(result));
    }
}
