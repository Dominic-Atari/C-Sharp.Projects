using System;

namespace Nile.MbUp;

public class PostUpdatedRequest : Nile.Common.InternalDTOs.RequestBase
{
    public Guid PostId { get; set; }
}
