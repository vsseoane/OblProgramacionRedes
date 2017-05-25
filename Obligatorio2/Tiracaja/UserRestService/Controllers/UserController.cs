using ServerRemotingServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Remoting;
using System.Web.Http;

namespace UserRestService.Controllers
{
    public class UserController : ApiController
    {

        public UserController() { }

        [HttpGet]
        [Route("api/users/addUser")]
        public void AddUser()
        {

            RemotingConfiguration.RegisterWellKnownClientType(Type.GetType("ServerRemotingServices.UserServiceRemoting, ServerRemotingServices"),
               "http://127.0.0.1:2223/userService");
            UserServiceRemoting service = new UserServiceRemoting();
            service.AddUser("prueba, tendria que mandar el objeto serializado");
        }
    }
}
