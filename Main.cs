using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using HttpMultipartParser;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using PKHeX.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace PKHeXLambda
{
    public class Functions
    {
        /// <summary>
        /// Local testing
        /// </summary>
        static void Main(string[] args) {
            var base64PKX = args.Length == 0 ? "" : args[0];
            var json = ConvertBase64PKXToJSON(base64PKX);
            Console.WriteLine(json.ToString());
        }

        /// <summary>
        /// Creates a JSON from a base 64 encoded PKX
        /// </summary>
        static private JObject ConvertBase64PKXToJSON(string base64PKX) {
            byte[] data = System.Convert.FromBase64String(base64PKX);
            var pkx = PKMConverter.GetPKMfromBytes(data) ?? new PK8();
            var legalityAnalysis = new LegalityAnalysis(pkx);
            var jPKX = (JObject)JToken.FromObject(pkx);
            var json = new JObject();
            bool isLegal = legalityAnalysis.Report(false) == "Legal!";

            jPKX.Add("IsLegal", isLegal);
            json.Add("pkx", jPKX);
            json.Add("signature", SignData(jPKX.ToString(Formatting.None)));

            return json;
        }

        /// <summary>
        /// Signs a string given to it
        /// </summary>
        static private string SignData(string data) {
            var sha1 = new SHA1Managed();
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(data));
            // This should be replaced with Secrets Manager
            string pem = Environment.GetEnvironmentVariable("PRIVATE_KEY").Replace("\\n", "\n");
            var pr = new PemReader(new StringReader(pem));
            var KeyPair = (RsaPrivateCrtKeyParameters)pr.ReadObject();
            var rsaParams = DotNetUtilities.ToRSAParameters((RsaPrivateCrtKeyParameters)KeyPair);
            var rsa = new RSACryptoServiceProvider();
            
            rsa.ImportParameters(rsaParams);

            var rsaFormatter = new RSAPKCS1SignatureFormatter(rsa);

            rsaFormatter.SetHashAlgorithm("SHA1");

            var signedHash = rsaFormatter.CreateSignature(hash);

            return Convert.ToBase64String(signedHash);
        }


        /// <summary>
        /// A Lambda function to respond to HTTP methods from API Gateway
        /// </summary>
        public APIGatewayProxyResponse ConvertPKXWithLambda(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Body = "{}",
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };

            if (String.IsNullOrEmpty(request.Body)) return response;

            var requestBody = request.IsBase64Encoded ? Convert.FromBase64String(request.Body) : Encoding.UTF8.GetBytes(request.Body);
            var parser = MultipartFormDataParser.Parse(new MemoryStream(requestBody));
            var base64PKX = parser.GetParameterValue("pkx");

            if (String.IsNullOrEmpty(base64PKX)) return response;

            var json = ConvertBase64PKXToJSON(base64PKX);

            response.Body = json.ToString();
            response.StatusCode = (int)HttpStatusCode.OK;

            return response;
        }
    }
}
