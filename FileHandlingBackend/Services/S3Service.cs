using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using FileHandlingBackend.Dtos;
using FileHandlingBackend.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FileHandlingBackend.Services
{
    public class S3Service:IS3Interface
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IModelInterface _modelService;
        private IConfiguration _configuration;
        private static string Bucket, PreviewBucket, AssetsBucket;

        public S3Service(IAmazonS3 s3Client, IModelInterface modelService, IConfiguration configuration)
        {
            _modelService = modelService;
            var awsOptions = configuration.GetSection("AWS");
            var accessKey = awsOptions["AccessKey"];
            var secretKey = awsOptions["SecretKey"];
            var region = awsOptions["Region"];
            Bucket = awsOptions["Bucket"];
            PreviewBucket = awsOptions["PreviewBucket"];
            AssetsBucket = awsOptions["AssetsBucket"];
            var credentials = new BasicAWSCredentials(accessKey, secretKey);
            _s3Client = new AmazonS3Client(credentials, RegionEndpoint.GetBySystemName(region));
        }

        public string GeneratePresentationName(string? projectFolder, string Title, int userID, FileType type)
        {
            var r = GenerateRandomCharacters();
            if (!String.IsNullOrEmpty(projectFolder))
                return $"Projects/{projectFolder}{Title}{userID}{r}.{type.ToString().ToLower()}";
            return $"Projects/{userID}/{projectFolder}{Title}{userID}{r}.{type.ToString().ToLower()}";
        }

        public async Task<PutObjectResponse> UploadFileAnyBucket(string userID, IFormFile file, string fileName, string bucket)
        {
            if (!BucketResponds())
                return null;

            string folderPath = $"{userID}/{fileName}";

            if (bucket == AssetsBucket)
                folderPath = $"{fileName}";

            var request = new PutObjectRequest()
            {
                BucketName = bucket,
                Key = folderPath,
                InputStream = file.OpenReadStream()
            };

            request.Metadata.Add("Content-Type", file.ContentType);
            var result = await _s3Client.PutObjectAsync(request);
            return result;
        }

        public async Task<S3ObjectDto?> UpdateScene(string s3Key, IFormFile file)
        {
            if (!BucketResponds())
                return null;

            try
            {
                using (var stream = file.OpenReadStream())
                {
                    var putRequest = new PutObjectRequest
                    {
                        BucketName = AssetsBucket,
                        Key = s3Key,
                        InputStream = stream,
                        ContentType = "application/json"
                    };

                    await _s3Client.PutObjectAsync(putRequest);
                }

                var metadataRequest = new GetObjectMetadataRequest
                {
                    BucketName = AssetsBucket,
                    Key = s3Key
                };

                var metadataResponse = await _s3Client.GetObjectMetadataAsync(metadataRequest);
                string presignedUrl = GenerateLongPresignedUrl(AssetsBucket, s3Key);

                return new S3ObjectDto
                {
                    Name = s3Key,
                    LastModified = metadataResponse.LastModified,
                    PresignedUrl = presignedUrl
                };
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine($"AmazonS3 Exception: {e.Message}");
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine($"General Exception: {e.Message}");
                return null;
            }
        }

        public async Task<List<S3ObjectDto>> GetAllJsonFilesInWebFolder(string projectFolder)
        {
            if (!BucketResponds())
                return null;

            var request = new ListObjectsV2Request
            {
                BucketName = AssetsBucket,
                Prefix = "Projects/" + projectFolder
            };

            var fileList = new List<S3ObjectDto>();

            try
            {
                ListObjectsV2Response response;
                do
                {
                    response = await _s3Client.ListObjectsV2Async(request);

                    foreach (var s3Object in response.S3Objects)
                    {
                        if (s3Object.Key.EndsWith(".json"))
                        {
                            string presignedUrl = GenerateLongPresignedUrl(AssetsBucket, s3Object.Key);

                            fileList.Add(new S3ObjectDto
                            {
                                Name = s3Object.Key,
                                LastModified = s3Object.LastModified,
                                PresignedUrl = presignedUrl
                            });
                        }
                    }

                    request.ContinuationToken = response.NextContinuationToken;

                } while (response.IsTruncated);
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine($"S3 error: {e.Message}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unknown error: {e.Message}");
            }

            return fileList.ToList();
        }

        public async Task<string> GetJsonFileWithUrl(string jsonUrl)
        {
            using (var httpClient = new HttpClient())
            {
                try
                {
                    var jsonContent = await httpClient.GetStringAsync(jsonUrl);
                    var jsonObject = JObject.Parse(jsonContent);

                    foreach (var slide in jsonObject["slides"]!)
                    {
                        foreach (var model in slide["models"]!)
                        {
                            var modelId = model["id"]!.ToString();
                            int parsedId = int.Parse(modelId);
                            var s3Key = await _modelService.GetS3KeyFromModelId(parsedId);
                            string newUrl = GenerateLongPresignedUrl(GetBucket(), s3Key);
                            model["url"] = newUrl;
                        }
                    }

                    return jsonObject.ToString(Formatting.Indented);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading/parsing JSON: {ex.Message}");
                    return null;
                }
            }
        }

        public async Task<List<S3ObjectDto>> GetAllFilesForUserAnyBucket(string userID, string bucket)
        {
            string folderPrefix = $"{userID}/";
            var request = new ListObjectsV2Request
            {
                BucketName = bucket,
                Prefix = folderPrefix
            };

            var fileList = new List<S3ObjectDto>();

            try
            {
                ListObjectsV2Response response;
                do
                {
                    response = await _s3Client.ListObjectsV2Async(request);

                    foreach (var s3Object in response.S3Objects)
                    {
                        string presignedUrl = GenerateLongPresignedUrl(bucket, s3Object.Key);

                        fileList.Add(new S3ObjectDto
                        {
                            Name = s3Object.Key,
                            LastModified = s3Object.LastModified,
                            PresignedUrl = presignedUrl
                        });
                    }

                    request.ContinuationToken = response.NextContinuationToken;

                } while (response.IsTruncated);
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine($"Error encountered on server. Message:'{e.Message}' when listing objects.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unknown error occurred. Message:'{e.Message}' when listing objects.");
            }

            fileList = fileList.OrderByDescending(x => x.LastModified).ToList();
            return fileList;
        }

        public async Task<List<S3ObjectDto>> GetAllFilesForPublicModels(string bucket, List<string> s3Keys)
        {
            var fileList = new List<S3ObjectDto>();

            foreach (var key in s3Keys)
            {
                try
                {
                    await _s3Client.GetObjectMetadataAsync(bucket, key);
                    string presignedUrl = GenerateLongPresignedUrl(bucket, key);

                    fileList.Add(new S3ObjectDto
                    {
                        Name = key,
                        PresignedUrl = presignedUrl
                    });
                }
                catch (AmazonS3Exception e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    fileList.Add(new S3ObjectDto
                    {
                        Name = key,
                        PresignedUrl = "N/A"
                    });
                }
            }

            return fileList;
        }

        public async Task<string> CreateProjectFolder(string projectName, int userID, string bucket)
        {
            string folderKey = $"Projects/{userID}/{projectName}{userID}/";
            var result = await CheckIfProjectFolderExists(folderKey, bucket);

            if (result)
                return null;

            var request = new PutObjectRequest
            {
                BucketName = bucket,
                Key = folderKey,
                ContentBody = string.Empty
            };

            try
            {
                await _s3Client.PutObjectAsync(request);
                Console.WriteLine($"Folder '{folderKey}' created successfully in bucket '{bucket}'.");
                return $"Folder '{folderKey}' created successfully in bucket '{bucket}'.";
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine($"Error encountered on server. Message:'{e.Message}' when creating folder.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unknown error occurred. Message:'{e.Message}' when creating folder.");
            }

            return "creation failed";
        }

        public string GenerateModelName(string Title, int userID, FileType type)
        {
            var r = GenerateRandomCharacters();
            return $"{Title}{userID.ToString()}{r}.{type.ToString().ToLower()}";
        }

        private async Task<bool> CheckIfProjectFolderExists(string folder, string bucket)
        {
            string folderPrefix = folder;
            var request = new ListObjectsV2Request
            {
                BucketName = bucket,
                Prefix = folderPrefix,
                MaxKeys = 1
            };

            try
            {
                ListObjectsV2Response response = await _s3Client.ListObjectsV2Async(request);
                return response.S3Objects.Count > 0;
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine($"Error encountered on server. Message:'{e.Message}' when checking folder existence.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unknown error occurred. Message:'{e.Message}' when checking folder existence.");
            }

            return false;
        }

        private string GenerateRandomCharacters()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random random = new Random();
            char[] result = new char[3];

            for (int i = 0; i < 3; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }

            return new string(result);
        }

        private bool BucketResponds()
        {
            var bucketExists = Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, Bucket).Result;
            return bucketExists;
        }

        public string GetAssetsBucket()
        {
            return AssetsBucket;
        }
        public string GetBucket()
        {
            return Bucket;
        }

        private string GenerateLongPresignedUrl(string bucket, string objectKey)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucket,
                Key = objectKey,
                Expires = DateTime.UtcNow.AddHours(6),
                Verb = HttpVerb.GET
            };

            return _s3Client.GetPreSignedURL(request);
        }
    }
}
