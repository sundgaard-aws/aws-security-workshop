import * as Core from '@aws-cdk/core';
import { MetaData } from './meta-data';
import { SSMHelper } from './ssm-helper';
import { IKey, Key } from '@aws-cdk/aws-kms';
import { ISecurityGroup, IVpc, SecurityGroup } from '@aws-cdk/aws-ec2';
import IAM = require("@aws-cdk/aws-iam");

export class SecurityStack extends Core.Stack {
    private ssmHelper = new SSMHelper();
    public cmk:IKey;
    public ApiSecurityGroup: ISecurityGroup;
    public ApiRole:IAM.IRole;
    constructor(scope: Core.Construct, id: string, vpc:IVpc, props?: Core.StackProps) {
        super(scope, id, props);
        this.cmk=this.createCustomerManagedKey();
        this.ApiSecurityGroup = this.createAPISecurityGroup(vpc);
        this.ApiRole = this.buildAPIRole();
    }    
    
    private createCustomerManagedKey(): IKey {
        var name=MetaData.PREFIX+"key";
        var key=new Key(this, name, {
            description: name,
            enabled: true            
        });
        Core.Tags.of(key).add(MetaData.NAME, name);
        return key;
    }

    private createAPISecurityGroup(vpc: IVpc): ISecurityGroup {
        var postFix = "api-sg";
        var securityGroup = new SecurityGroup(this, MetaData.PREFIX+postFix, {
            vpc: vpc,
            securityGroupName: MetaData.PREFIX+postFix,
            description: MetaData.PREFIX+postFix,
            allowAllOutbound: true
        });
        
        //securityGroup.connections.allowTo(this.metaData.RDSSecurityGroup, EC2.Port.tcp(3306), "Lambda to RDS");
        Core.Tags.of(securityGroup).add(MetaData.NAME, MetaData.PREFIX+postFix);
        //this.metaData.APISecurityGroup = securityGroup;
        return securityGroup;
    }

    private buildAPIRole(): IAM.IRole {
        var role = new IAM.Role(this, MetaData.PREFIX+"api-role", {
            description: "Lambda API Role",
            roleName: MetaData.PREFIX+"api-role",
            assumedBy: new IAM.ServicePrincipal("lambda.amazonaws.com"),
            managedPolicies: [
                IAM.ManagedPolicy.fromAwsManagedPolicyName("AWSStepFunctionsFullAccess"),
                IAM.ManagedPolicy.fromAwsManagedPolicyName("AmazonSSMFullAccess"),
                IAM.ManagedPolicy.fromManagedPolicyArn(this, "AWSLambdaSQSQueueExecutionRole", "arn:aws:iam::aws:policy/service-role/AWSLambdaSQSQueueExecutionRole"),
                IAM.ManagedPolicy.fromManagedPolicyArn(this, "AWSLambdaBasicExecutionRole", "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"),
                IAM.ManagedPolicy.fromManagedPolicyArn(this, "AWSLambdaVPCAccessExecutionRole", "arn:aws:iam::aws:policy/service-role/AWSLambdaVPCAccessExecutionRole")
            ],
        });
        role.addToPolicy(new IAM.PolicyStatement({
          effect: IAM.Effect.ALLOW,
          resources: ["*"],
          actions: ["secretsmanager:GetSecretValue","dbqms:*","rds-data:*","xray:*","dynamodb:GetItem","dynamodb:PutItem","dynamodb:UpdateItem","dynamodb:Scan","dynamodb:Query"]
        }));

        Core.Tags.of(role).add(MetaData.NAME, MetaData.PREFIX+"api-role");
        return role;
    } 
}