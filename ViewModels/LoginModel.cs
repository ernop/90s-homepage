using System;
using System.Linq;

using System.Collections.Generic;

namespace FusekiC
{
    public class LoginModel
    {
        public LoginModel() { }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Error { get; set; } = "";
    }
}