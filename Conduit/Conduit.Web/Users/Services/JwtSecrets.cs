﻿using System.Text;

namespace Conduit.Web.Users.Services;

public class JwtSecrets
{
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public string SecurityKey { get; set; }
}