//C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5.2

#r "<Your Local Path>\getHashKey\lib\System.IdentityModel.dll"

using System.Net;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IdentityModel.Tokens;
using System.ServiceModel.Security.Tokens;

///Main of the Function
public static HttpResponseMessage Run(HttpRequestMessage req, TraceWriter log)
{
    try
    {
        //Gettings parameters
        var data = req.Content.ReadAsStringAsync().Result;
        log.Info(data);
        var formValues = data.Split('&')
            .Select(value => value.Split('='))
            .ToDictionary(pair => Uri.UnescapeDataString(pair[0]).Replace("+", " "),
                          pair => Uri.UnescapeDataString(pair[1]).Replace("+", " "));


        var issuedToken = formValues["token"];
        var account = formValues["account"];
        log.Info(issuedToken);
 
        //Check the token, and compare with the UserId
        var tokenuserId = checkTokenValidity(issuedToken);

        if (tokenuserId != null)
        {
            //hash the User Key
            string hash = getHashKey(tokenuserId + ":" + account);
            //return the hash key
            return req.CreateResponse(HttpStatusCode.OK, hash);
        }
        else
        {
            return req.CreateResponse(HttpStatusCode.InternalServerError, "The token is not valid");
        }
    }
    catch(Exception ex)
    {
        return req.CreateResponse(HttpStatusCode.InternalServerError, ex);
    }
}

//Check the user token validity and return the userId
public static string checkTokenValidity(string issuedToken)
{
    try
    {
        string secret = "<Your VSTS extension Certificate>"; // Load your extension's secret

        var validationParameters = new TokenValidationParameters()
        {
            IssuerSigningTokens = new List<BinarySecretSecurityToken>()
                        {
                            new BinarySecretSecurityToken (System.Text.UTF8Encoding.UTF8.GetBytes(secret))
                        },
            ValidateIssuer = false,
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ValidateAudience = false,
            ValidateActor = false
        };

        SecurityToken token = null;
        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(issuedToken, validationParameters, out token);
        //compare the principal with the userId
        string principalUserId = principal.Claims.FirstOrDefault(q => string.Compare(q.Type, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", true) == 0).Value;

        return principalUserId;

    }
    catch
    {
        return null;
    }
}


//Hash the secure userkey
public static string getHashKey(string userkey)
{
    if (string.IsNullOrEmpty(userkey))
    {
        return null;
    }
    System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
    byte[] keyBytes = encoding.GetBytes("<Your LaunchDarkly SDK Key>");

    HMACSHA256 hmacSha256 = new HMACSHA256(keyBytes);
    byte[] hashedMessage = hmacSha256.ComputeHash(encoding.GetBytes(userkey));
    return BitConverter.ToString(hashedMessage).Replace("-", "").ToLower();
}