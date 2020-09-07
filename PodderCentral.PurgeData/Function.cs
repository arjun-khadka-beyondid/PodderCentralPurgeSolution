using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PodderCentral.PurgeData
{
    public class Function
    {

        private ConfigSetting _configSetting;
        public Function()
        {
            _configSetting = new ConfigSetting();
            var environmentByte = File.ReadAllBytes("Config.json");
            MemoryStream streamEnv = new MemoryStream(environmentByte);
            var serializer = new DefaultLambdaJsonSerializer();
            var environmentSetting = serializer.Deserialize<EnvironmentSetting>(streamEnv);
            if (!environmentSetting.IsCloud)
            {

                var configSettingByte = File.ReadAllBytes($"Config.{environmentSetting.Environment}.json");
                var streamConfigSetting = new MemoryStream(configSettingByte);

                _configSetting = serializer.Deserialize<ConfigSetting>(streamConfigSetting);

            }
            else
            {
                _configSetting.Region = Environment.GetEnvironmentVariable("region");
                _configSetting.SecretName = Environment.GetEnvironmentVariable("secretName");
                _configSetting.DbName = Environment.GetEnvironmentVariable("dbName");
            }
        }

        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public  async Task<string> FunctionHandler(ILambdaContext context)
        {
            try
            {

                var secret = await GetSecret();
                if (string.IsNullOrWhiteSpace(secret)) throw new ArgumentException("Secret string is null");
                var serializer = new DefaultLambdaJsonSerializer();
                byte[] byteArray = Encoding.ASCII.GetBytes(secret);
                MemoryStream stream = new MemoryStream(byteArray);
                var secretCredential = serializer.Deserialize<DBSecretCredential>(stream);
                secretCredential.DbName = _configSetting.DbName;

                var dbContext = new DBContext(secretCredential);               
                int? affRows = null;
                affRows = dbContext.PurgeData();

                //return $"AffectedRows:{affRows}";
                var result = $"Success:true, AffectedRows:{affRows}{Environment.NewLine}";
                LambdaLogger.Log(result);
                return result;

            }
            catch (Exception ex)
            {
                 var errorMessage = $"Error:{ex.Message}";
                LambdaLogger.Log($"{errorMessage}, Stacktrace: {ex.StackTrace}{Environment.NewLine}");
                return errorMessage;
            }
        }
        private async Task<string> GetSecret()
        {           
            string secretName = _configSetting?.SecretName;
            string region = _configSetting?.Region;
            if (string.IsNullOrEmpty(secretName) || string.IsNullOrEmpty(region)) throw new ApplicationException("SecretName and Region do not exist");

            string secret = "";
            LambdaLogger.Log("Region " + region + Environment.NewLine);
            //LambdaLogger.Log("SecretName " + secretName);          

            LambdaLogger.Log($"Started:GetBySystemName{Environment.NewLine}");
            IAmazonSecretsManager client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));
            LambdaLogger.Log($"Ended:GetBySystemName{Environment.NewLine}");
            GetSecretValueRequest request = new GetSecretValueRequest();
            request.SecretId = secretName;
            request.VersionStage = "AWSCURRENT"; // VersionStage defaults to AWSCURRENT if unspecified.

            GetSecretValueResponse response = null;

            // In this sample we only handle the specific exceptions for the 'GetSecretValue' API.
            // See https://docs.aws.amazon.com/secretsmanager/latest/apireference/API_GetSecretValue.html
            // We rethrow the exception by default.

            try
            {
                LambdaLogger.Log($"Started:GetSecretValueAsync{Environment.NewLine}");
                response = await client.GetSecretValueAsync(request);
                LambdaLogger.Log($"Ended: GetSecretValueAsync{Environment.NewLine}");

            }
            catch (DecryptionFailureException ex)
            {
                // Secrets Manager can't decrypt the protected secret text using the provided KMS key.
                // Deal with the exception here, and/or rethrow at your discretion.
                LambdaLogger.Log($"Exception:{ex.Message}, Stacktrace: {ex.StackTrace}{Environment.NewLine}");

            }
            catch (InternalServiceErrorException ex)
            {
                // An error occurred on the server side.
                // Deal with the exception here, and/or rethrow at your discretion.
                LambdaLogger.Log($"Exception:{ex.Message}, Stacktrace: {ex.StackTrace}{Environment.NewLine}");

            }
            catch (InvalidParameterException ex)
            {
                // You provided an invalid value for a parameter.
                // Deal with the exception here, and/or rethrow at your discretion
                LambdaLogger.Log($"Exception:{ex.Message}, Stacktrace: {ex.StackTrace}{Environment.NewLine}");

            }
            catch (InvalidRequestException ex)
            {
                // You provided a parameter value that is not valid for the current state of the resource.
                // Deal with the exception here, and/or rethrow at your discretion.
                LambdaLogger.Log($"Exception:{ex.Message}, Stacktrace: {ex.StackTrace}{Environment.NewLine}");

            }
            catch (ResourceNotFoundException ex)
            {
                // We can't find the resource that you asked for.
                // Deal with the exception here, and/or rethrow at your discretion.
                LambdaLogger.Log($"Exception:{ex.Message}, Stacktrace: {ex.StackTrace}{Environment.NewLine}");

            }
            catch (AggregateException ex)
            {
                // More than one of the above exceptions were triggered.
                // Deal with the exception here, and/or rethrow at your discretion.
                LambdaLogger.Log($"Exception:{ex.Message}, Stacktrace: {ex.StackTrace}{Environment.NewLine}");

            }

            // Decrypts secret using the associated KMS CMK.
            // Depending on whether the secret is a string or binary, one of these fields will be populated.
            if (response.SecretString != null)
            {
                secret = response.SecretString;
               
            }
            else
            {
                MemoryStream memoryStream = new MemoryStream();
                memoryStream = response.SecretBinary;
                StreamReader reader = new StreamReader(memoryStream);
                string decodedBinarySecret = Encoding.UTF8.GetString(Convert.FromBase64String(reader.ReadToEnd()));
              
            }

            // Your code goes here.
            return secret;
        }
        private async Task<string> GetFakeSecret()
        {
            var secret = "{\"username\":\"postgres\",\"password\":\"Beyond123$\",\"engine\":\"postgres\",\"host\":\"postgre-database-1.cluster-c6nj7tjbxrx6.us-east-1.rds.amazonaws.com\",\"port\":5432,\"dbClusterIdentifier\":\"postgre-database-1\"}";
            return secret;
        }
    }

    
}
