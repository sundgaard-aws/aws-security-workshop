import * as Core from '@aws-cdk/core';
import EC2 = require('@aws-cdk/aws-ec2');
import S3 = require('@aws-cdk/aws-s3');
import Lambda = require('@aws-cdk/aws-lambda');
import { ISecurityGroup, IVpc } from '@aws-cdk/aws-ec2';
import { MetaData } from './meta-data';
import { SSMHelper } from './ssm-helper';
import { HttpApi, HttpMethod } from '@aws-cdk/aws-apigatewayv2';
import { HttpLambdaIntegration } from '@aws-cdk/aws-apigatewayv2-integrations';
import { IKey } from '@aws-cdk/aws-kms';
import IAM = require("@aws-cdk/aws-iam");

export class ComputeStack extends Core.Stack {
    private runtime:Lambda.Runtime = Lambda.Runtime.DOTNET_6;
    private cmk:IKey;
    private apiRole:IAM.IRole;

    constructor(scope: Core.Construct, id: string, vpc: IVpc, apiSecurityGroup: ISecurityGroup, apiRole:IAM.IRole, cmk:IKey, props?: Core.StackProps) {
        super(scope, id, props);
        this.apiRole=apiRole;
        //this.createArchivePaymentRequestFunction(apiSecurityGroup, vpc);
        this.createSendExternalPaymentRequestFunction(apiSecurityGroup, vpc);
        this.createReceiveExternalPaymentResponseFunction(apiSecurityGroup, vpc);
        this.createProPayRequestFunction(apiSecurityGroup, vpc);
        this.createProPayResponseFunction(apiSecurityGroup, vpc);
        this.createProPayGenerateKeyPairFunction(apiSecurityGroup, vpc);
    }

    private createLambdaFunction(apiSecurityGroup: ISecurityGroup, name:string, handlerMethod:string, assetPath:string, vpc:EC2.IVpc):Lambda.Function {
        var codeFromLocalZip = Lambda.Code.fromAsset(assetPath);
        var lambdaFunction = new Lambda.Function(this, MetaData.PREFIX+name, { 
            functionName: MetaData.PREFIX+name, vpc: vpc, code: codeFromLocalZip, handler: handlerMethod, runtime: this.runtime, memorySize: 256, 
            timeout: Core.Duration.seconds(10), role: this.apiRole, securityGroups: [apiSecurityGroup],
            tracing: Lambda.Tracing.ACTIVE,
            environmentEncryption: this.cmk
        });
        
        const lambdaIntegration = new HttpLambdaIntegration(MetaData.PREFIX+name+"-lam-int", lambdaFunction);        
        const httpApi = new HttpApi(this, MetaData.PREFIX+name+"-api");
        
        httpApi.addRoutes({
        path: "/" + name,
        methods: [ HttpMethod.POST, HttpMethod.OPTIONS ],
        integration: lambdaIntegration,
        });
        
        Core.Tags.of(lambdaFunction).add(MetaData.NAME, MetaData.PREFIX+name);
        return lambdaFunction;
    } 
    
    private createProPayGenerateKeyPairFunction(apiSecurityGroup: ISecurityGroup, vpc: IVpc):Lambda.Function {
        return this.createLambdaFunction(apiSecurityGroup, "ProPayGenerateKeyPairFunction", "ProPayGenerateKeyPairHandler::OM.AWS.Demo.API.FunctionHandler::Invoke", "../code/ProPayGenerateKeyPairHandler/publish/", vpc);
    }

    private createProPayRequestFunction(apiSecurityGroup: ISecurityGroup, vpc: IVpc):Lambda.Function {
        return this.createLambdaFunction(apiSecurityGroup, "ProPayRequestFunction", "ProPayRequestHandler::OM.AWS.Demo.API.FunctionHandler::Invoke", "../code/ProPayRequestHandler/publish/", vpc);
    }

    private createProPayResponseFunction(apiSecurityGroup: ISecurityGroup, vpc: IVpc):Lambda.Function {
        return this.createLambdaFunction(apiSecurityGroup, "ProPayResponseFunction", "ProPayResponseHandler::OM.AWS.Demo.API.FunctionHandler::Invoke", "../code/ProPayResponseHandler/publish/", vpc);
    }

    private createSendExternalPaymentRequestFunction(apiSecurityGroup: ISecurityGroup, vpc: IVpc):Lambda.Function {
        return this.createLambdaFunction(apiSecurityGroup, "SendExternalPaymentRequestFunction", "PaymentRequestHandler::OM.AWS.Demo.API.FunctionHandler::Invoke", "../code/PaymentRequestHandler/publish/", vpc);
    }

    private createReceiveExternalPaymentResponseFunction(apiSecurityGroup: ISecurityGroup, vpc: IVpc):Lambda.Function {
        return this.createLambdaFunction(apiSecurityGroup, "ReceiveExternalPaymentResponseFunction", "PaymentResponseHandler::OM.AWS.Demo.API.FunctionHandler::Invoke", "../code/PaymentResponseHandler/publish/", vpc);
    }

    private createLambdaCodeBucket()
    {
        var codeBucket = new S3.Bucket(this, MetaData.PREFIX+"lambda-code-bucket", {
            bucketName: MetaData.PREFIX+"lambda-code-bucket", removalPolicy: Core.RemovalPolicy.DESTROY
        });
        Core.Tags.of(codeBucket).add(MetaData.NAME, MetaData.PREFIX+"lambda-code-bucket");
        //this.ssmHelper.createSSMParameter(this, MetaData.PREFIX+"state-machine-arn", stateMachine.stateMachineArn, SSM.ParameterType.STRING);
    }    
}