using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using N.LMS.Client.WebApi.Extension;
using N.LMS.Client.WebApi.Request.Admin;
using N.LMS.Client.WebApi.Response.Admin;
using N.LMS.Common.Service;
using N.LMS.Manager.Admin.Interface;
using AdminReq = N.LMS.Manager.Admin.Interface.Request;
using AdminRes = N.LMS.Manager.Admin.Interface.Result;

namespace N.LMS.Client.WebApi.ApiControllers;

[ApiController]
[Route("[controller]")]
public class PersonController : ApiControllerBase
{
    [HttpPost("Load")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Load([FromBody] PersonLoadRequest request)
    {
        using var proxy = new ServiceProxyGenerator(HttpContext.ToWebContext());

        var result = (AdminRes.PersonLoadResult)await proxy.ProxyForService<IAdminManager>()
            .Load(new AdminReq.PersonLoadRequest { PersonId = request.PersonId });

        return CreateActionResult(Mapper.Map<PersonLoadResponse>(result));
    }

    [HttpPost("Store")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Store([FromBody] PersonStoreRequest request)
    {
        using var proxy = new ServiceProxyGenerator(HttpContext.ToWebContext());

        var result = (AdminRes.PersonStoreResult)await proxy.ProxyForService<IAdminManager>()
            .Store(new AdminReq.PersonStoreRequest
            {
                PersonId = request.PersonId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Password = request.Password,
                Role = request.Role,
                CohortId = request.CohortId
            });

        return CreateActionResult(Mapper.Map<PersonStoreResponse>(result));
    }

    [HttpDelete]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete([FromBody] PersonDeleteRequest request)
    {
        using var proxy = new ServiceProxyGenerator(HttpContext.ToWebContext());

        var result = (AdminRes.PersonDeleteResult)await proxy.ProxyForService<IAdminManager>()
            .Delete(new AdminReq.PersonDeleteRequest { PersonId = request.PersonId });

        return CreateActionResult(Mapper.Map<PersonDeleteResponse>(result));
    }
}
