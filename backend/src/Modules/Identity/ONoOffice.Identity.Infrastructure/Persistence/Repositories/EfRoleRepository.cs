using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.Infrastructure.Persistence.Repositories;

internal sealed class EfRoleRepository(IdentityDbContext context) : IRoleRepository
{
    public void AddRange(IEnumerable<Role> roles) => context.Roles.AddRange(roles);
}
