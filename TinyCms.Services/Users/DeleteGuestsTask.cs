using System;
using TinyCms.Services.Tasks;

namespace TinyCms.Services.Users
{
    /// <summary>
    /// Represents a task for deleting guest users
    /// </summary>
    public partial class DeleteGuestsTask : ITask
    {
        private readonly IUserService _userService;

        public DeleteGuestsTask(IUserService userService)
        {
            this._userService = userService;
        }

        /// <summary>
        /// Executes a task
        /// </summary>
        public void Execute()
        {
            //60*24 = 1 day
            var olderThanMinutes = 1440; //TODO move to settings
            //Do not delete more than 1000 records per time. This way the system is not slowed down
            _userService.DeleteGuestUsers(null, DateTime.UtcNow.AddMinutes(-olderThanMinutes));
        }
    }
}
