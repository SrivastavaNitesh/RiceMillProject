using Microsoft.Extensions.Configuration;
using RiceMillProject.DAL;
using RiceMillProject.Models;

namespace RiceMillProject.BAL
{
    public class UserBAL
    {
        private readonly UserDAL _userDal;

        public UserBAL(IConfiguration configuration)
        {
            _userDal = new UserDAL(configuration);
        }

        public User? ValidateUser(string username, string password)
        {
            return _userDal.ValidateUser(username, password);
        }
    }
}
