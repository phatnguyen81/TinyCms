using TinyCms.Core.Domain.Users;

namespace TinyCms.Services.Authentication
{
    /// <summary>
    /// Authentication service interface
    /// </summary>
    public partial interface IAuthenticationService 
    {
        void SignIn(User user, bool createPersistentCookie);
        void SignOut();
        User GetAuthenticatedUser();
    }
}