using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using RiceMillProject.DAL;
using RiceMillProject.Models;

namespace RiceMillProject.BAL
{
    public class PersonBAL
    {
        private readonly PersonDAL _personDal;

        public PersonBAL(IConfiguration configuration)
        {
            _personDal = new PersonDAL(configuration);
        }

        public List<Person> GetAllPersons()
        {
            return _personDal.GetAllPersons();
        }
        public List<Person> GetEmployeeList(string Mode)
        {
            return _personDal.GetEmployeeList(Mode);
        }
        public bool IsMobileNumberUnique(string mobileNumber, int excludePersonId = 0)
        {
            return _personDal.IsMobileNumberUnique(mobileNumber, excludePersonId);
        }

        public int AddPerson(Person person)
        {
            return _personDal.InsertPerson(person);
        }

        public bool UpdatePerson(Person person)
        {
            return _personDal.UpdatePerson(person);
        }
    }
}
