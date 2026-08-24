using Microsoft.AspNetCore.Authorization;

namespace ONoOffice.Api.Authorization;

/// <summary>
/// "Muốn vào chỗ này thì phải có quyền <paramref name="Permission"/>."
///
/// Chỉ là một cái tên quyền được gói lại cho ASP.NET hiểu. Toàn bộ phần quyết định
/// nằm ở <see cref="PermissionAuthorizationHandler"/>.
/// </summary>
public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;
