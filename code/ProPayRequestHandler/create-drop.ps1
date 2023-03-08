clear
#echo Compressing function code...
#zip -rq drop.zip .
#7z a -r C:\github\dotnet-lambda-function\SimpleFunctionHandler\drop.zip .\bin\debug\netcoreapp3.1\publish\*
#7z a -r .\LambdaHandler.zip .\bin\debug\net6.0\publish\*

#echo Uploading to Code Artifactory...
#dotnet pack
#aws codeartifact login --tool dotnet --domain "dotnet-repo"  --repository dotnet-repo --region eu-north-1
#dotnet nuget push .\bin\Debug\lambda-handler.1.0.0.nupkg --source "dotnet-repo/dotnet-repo"
echo "Publishing code..."
dotnet publish -c Release
#7z a -r .\zip\FunctionHandler.zip .\publish\*


#aws lambda update-function-code --function-name $functionName --zip-file fileb://.\drop.zip
#echo Cleaning up...
#rm drop.zip
echo "Done."

#aws s3 cp invoke-sfn-api.zip s3://iac-demo-lambda-code-bucket
#rm invoke-sfn-api.zip

# Upload to Lambda
#s3://iac-demo-lambda-code-bucket/invoke-sfn-api.zip