//using System.Security.Claims;
//using Microsoft.Extensions.Configuration;
//using TokenGenerator;

//var configuration = new ConfigurationBuilder()
//    .AddJsonFile("appsettings.json")
//    .Build();

//var (token, tokenString) = TokenHelper.CreateToken(
//    "somereallylongkeygoeshere", 
//    new TokenHelper.CreateTokenParams {
//        Issuer = "jwmb",
//        Audience = "logsink_client",
//        Expiry = DateTime.UtcNow.AddYears(5),
//        //Claims = new[] { new Claim("iat", DateTime.UtcNow.ToEpochTime().ToString(), ClaimValueTypes.Integer64), },
//        ClaimsDictionary = new Dictionary<string, string> {
//            { "sub", "klingberglab" },
//            { "pipeline", "problemsource" },
//        }
//    });

//Console.WriteLine(tokenString);
//Console.ReadLine();

