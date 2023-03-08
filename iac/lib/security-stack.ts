import * as Core from '@aws-cdk/core';
import { MetaData } from './meta-data';
import { SSMHelper } from './ssm-helper';
import { IKey, Key } from '@aws-cdk/aws-kms';
import { ISecurityGroup, IVpc, Port, SecurityGroup } from '@aws-cdk/aws-ec2';
import IAM = require("@aws-cdk/aws-iam");

export class SecurityStack extends Core.Stack {
    private ssmHelper = new SSMHelper();
    
    constructor(scope: Core.Construct, id: string, vpc:IVpc, ssmVPCEndpointSG:ISecurityGroup, props?: Core.StackProps) {
        super(scope, id, props);
        
    }    
    
    
}