using Microsoft.AspNetCore.Mvc;

namespace Xdb.Core.LegacyWire;

/// <summary>
/// Base for controllers that reproduce the original service's operations. Deliberately not [ApiController]:
/// the original bound missing query values to defaults and had no automatic 400 responses.
/// Derived controllers declare [Route(LegacyWire.BaseRoute)].
/// </summary>
[TypeFilter(typeof(LegacyFaultFilter))]
[ApiExplorerSettings(GroupName = "legacy")]
public abstract class LegacyWireControllerBase : ControllerBase;
