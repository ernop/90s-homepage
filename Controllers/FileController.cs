using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Web;
using System.Text.RegularExpressions;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace FusekiC
{
    [Authorize]

    public class FileController : Controller
    {
        private static Regex AlphaNumeric = new Regex("^[a-zA-Z0-9]*$");

        private PublishConfiguration PublishConfiguration { get; set; }
        public FileController(PublishConfiguration pc)
        {
            PublishConfiguration = pc;
        }
    }
}
