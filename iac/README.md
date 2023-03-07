# Guide
For now sticking with the latest v1 is the better option and some v2 constructs are still in alpha.

https://docs.aws.amazon.com/cdk/api/v1/docs/

Upgrade to latest version of AWS CDK
``` ps
npm install -g aws-cdk@latest
```

First run the command below to install Typescript compiler (tcs) and other dependencies
``` ps
npm install
```

This is a blank project for TypeScript development with CDK.

The `cdk.json` file tells the CDK Toolkit how to execute your app.

## Useful commands

compile typescript to js
 ``` ps
 npm run build
 ```

 if this is the first time anyone run the CDK in this AWS account, the first run the command below to bootstrap the CDK.
``` ps
 npx cdk bootstrap
 ```
 
 deploy this stack to your default AWS account/region
 ``` ps
 cdk deploy --all
 ```
 or
 ``` ps
 npx cdk deploy --all
 ```

 * `npm run watch`   watch for changes and compile
 * `npm run test`    perform the jest unit tests
 
 * `cdk diff`        compare deployed stack with current state
 * `cdk synth`       emits the synthesized CloudFormation template
