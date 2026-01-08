#include "configuration.h"

namespace trace
{

bool sqlclient_netfx_ilrewrite_enabled = false;

bool IsSqlClientNetFxILRewriteEnabled()
{
    return sqlclient_netfx_ilrewrite_enabled;
}

void SetSqlClientNetFxILRewriteEnabled(bool enabled)
{
    sqlclient_netfx_ilrewrite_enabled = enabled;
}

} // namespace trace