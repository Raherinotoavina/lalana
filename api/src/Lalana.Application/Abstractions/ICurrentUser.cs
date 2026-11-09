using Lalana.Domain.Identity;

namespace Lalana.Application.Abstractions;

/// <summary>The member making the current request, if the request is authenticated.</summary>
public interface ICurrentUser
{
    UserId? Id { get; }
}
