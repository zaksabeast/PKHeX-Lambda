# PKHeX-Lambda

This is a quick project to expose PKX parsing through a REST API using AWS API Gateway and Lambda.

The result is signed so this can be used as a standalone service separate from any other service, and the result can be validated with a public key to ensure it wasn't tampered with.

Ideally this could be iterated on to use AWS Secrets Manager and a more reliable JSON signing method.

## Why?

PKHeX undoubtedly has the best PKX parsing and legality checking. Porting those features to other languages and maintaining them would be crazy, and I'm not sure there have been any completely successful efforts.

While it's possible to use a passthrough script which calls a PKHeX Core CLI, this method isn't very clean.

As a result, this was made to have an endpoint which can be called to parse and legality check a PKX.

## Building

### Without Docker

Install the dotnet tools, and the AWS Lambda tools:

- `dotnet tool install --global Amazon.Lambda.Tools`.

Release build:

- `sh build.sh`

Debug build:

- `dotnet build PKHeXLambda.csproj`

### With Docker

A docker image is provided for those who don't want to install the required tools for this on their host machine.

Build the docker image:

- `docker build -t dotnet .`

Rease build:

- `docker run --rm -it -v $(pwd):/app -w /app dotnet sh /app/build.sh`

For the debug build, it's a good idea to enter the container with a bash session so you can also use dotnet to run debug build:

- `docker run --rm -it -v $(pwd):/app -w /app dotnet /bin/bash`
- `dotnet build PKHeXLambda.csproj`

## How do I use this?

1. Create an AWS Lambda function using .NET Core 2.1
   - The handler will be `PKHeXLambda::PKHeXLambda.Functions::ConvertPKXWithLambda`
1. Upload the release zip to the Lambda
1. Generate a private key to set as the `PRIVATE_KEY` environment variable
1. Create an API Gateway with a route triggering the Lambda
1. Send https requests to the endpoint with a form-data body holding a `pkx` key with a base 64 encoded pkx
   - The result will be an JSON with a `pkx` and `signature` properties. `pkx` has all the properties PKHeX.Core would have as well as an `IsLegal` property
